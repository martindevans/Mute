using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluidCaching;
using JetBrains.Annotations;
using Mute.Moe.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mute.Moe.Services.Words
{
    public class HttpWordVectors
        : IWords
    {
        private readonly IHttpClient _client;
        private readonly WordVectorsConfig _config;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly FluidCache<WordVector> _vectorCache;
        private readonly IIndex<string, WordVector> _indexByWord;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly FluidCache<SimilarResult> _similarCache;
        private readonly IIndex<string, SimilarResult> _indexSimilarByWord;

        public HttpWordVectors([NotNull] Configuration config, IHttpClient client)
        {
            _client = client;
            _config = config.WordVectors;

            _vectorCache = new FluidCache<WordVector>((int)_config.CacheSize, TimeSpan.FromSeconds(_config.CacheMinTimeSeconds), TimeSpan.FromMinutes(_config.CacheMaxTimeSeconds), () => DateTime.UtcNow);
            _indexByWord = _vectorCache.AddIndex("byWord", a => a.Word);

            _similarCache = new FluidCache<SimilarResult>((int)_config.CacheSize, TimeSpan.FromSeconds(_config.CacheMinTimeSeconds), TimeSpan.FromMinutes(_config.CacheMaxTimeSeconds), () => DateTime.UtcNow);
            _indexSimilarByWord = _similarCache.AddIndex("byWord", a => a.Root);
        }

        public async Task<IReadOnlyList<float>> Vector(string word)
        {
            return (await GetVectorObject(word))?.Vector;
        }

        [ItemCanBeNull, NotNull]
        private async Task<WordVector> GetVectorObject(string word)
        {
            word = word.ToLowerInvariant();

            var item = await _indexByWord.GetItem(word, GetVectorNonCached);
            return item;
        }

        [ItemCanBeNull, NotNull]
        private async Task<WordVector> GetVectorNonCached(string word)
        {
            var url = new UriBuilder(_config.WordVectorsBaseUrl) {Path = $"get_vector/{Uri.EscapeUriString(word)}"};

            using (var resp = await _client.GetAsync(url.ToString()))
            {
                if (!resp.IsSuccessStatusCode)
                    return null;

                var arr = JArray.Parse(await resp.Content.ReadAsStringAsync());

                var vector = new List<float>(300);
                foreach (var jToken in arr)
                    vector.Add((float)jToken);

                return new WordVector(word, vector);
            }
        }

        public async Task<IReadOnlyList<ISimilarWord>> Similar(string word)
        {
            return (await GetSimilarObject(word))?.Similar;
        }

        [ItemCanBeNull, NotNull]
        private async Task<SimilarResult> GetSimilarObject(string word)
        {
            word = word.ToLowerInvariant();

            var item = await _indexSimilarByWord.GetItem(word, GetSimilarNonCached);
            return item;
        }

        [ItemCanBeNull, NotNull]
        private async Task<SimilarResult> GetSimilarNonCached(string word)
        {
            var url = new UriBuilder(_config.WordVectorsBaseUrl) {Path = $"get_similar/{Uri.EscapeUriString(word)}"};

            using (var resp = await _client.GetAsync(url.ToString()))
            {
                if (!resp.IsSuccessStatusCode)
                    return null;

                SimilarWord[] results;
                var serializer = new JsonSerializer();
                using (var sr = new StreamReader(await resp.Content.ReadAsStreamAsync()))
                using (var jsonTextReader = new JsonTextReader(sr))
                    results = serializer.Deserialize<SimilarWord[]>(jsonTextReader);

                return new SimilarResult(word, results);
            }
        }

        public async Task<double?> Similarity(string a, string b)
        {
            var av = await GetVectorObject(a);
            var bv = await GetVectorObject(b);

            if (av == null || bv == null)
                return null;

            var dot = 0.0;
            for (var i = 0; i < av.Vector.Count; i++)
                dot += av.Vector[i] * bv.Vector[i];

            return dot / (av.Length * bv.Length);
        }

        private class WordVector
        {
            public string Word { get; }
            public IReadOnlyList<float> Vector { get; }

            public WordVector(string word, IReadOnlyList<float> vector)
            {
                Word = word;
                Vector = vector;
            }

            private double? _length;
            public double Length
            {
                get
                {
                    if (_length == null)
                    {
                        var acc = 0.0;
                        foreach (var f in Vector)
                            acc += f * f;
                        _length = Math.Sqrt(acc);
                    }
                    return _length.Value;
                }
            }
        }

        private class SimilarResult
        {
            public string Root { get; }
            public IReadOnlyList<ISimilarWord> Similar { get; }

            public SimilarResult(string root, IReadOnlyList<ISimilarWord> similar)
            {
                Root = root;
                Similar = similar;
            }
        }

        private class SimilarWord
            : ISimilarWord
        {
            [UsedImplicitly, JsonProperty("word")] private string _word;
            public string Word => _word;

            [UsedImplicitly, JsonProperty("distance")] private float _distance;
            public float Similarity => 1 - Math.Max(0f, _distance);

            public SimilarWord(string word, float difference)
            {
                _word = word;
                _distance = difference;
            }
        }
    }
}
