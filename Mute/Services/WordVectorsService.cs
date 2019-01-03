using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluidCaching;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;

namespace Mute.Services
{
    public class WordVectorsService
    {
        private readonly IHttpClient _client;
        private readonly WordVectorsConfig _config;

        private readonly FluidCache<WordVector> _vectorCache;
        private readonly IIndex<string, WordVector> _indexByWord;

        public int CacheCount => _vectorCache.Statistics.Current;
        public long CacheHits => _vectorCache.Statistics.Hits;
        public long CacheMisses => _vectorCache.Statistics.Misses;

        public WordVectorsService([NotNull] Configuration config, IHttpClient client)
        {
            _client = client;
            _config = config.WordVectors;

            _vectorCache = new FluidCache<WordVector>((int)_config.CacheSize, TimeSpan.FromSeconds(_config.CacheMinTimeSeconds), TimeSpan.FromMinutes(_config.CacheMaxTimeSeconds), () => DateTime.UtcNow);
            _indexByWord = _vectorCache.AddIndex("byWord", a => a.Word);
        }

        [ItemCanBeNull, NotNull]
        public async Task<IReadOnlyList<float>> GetVector(string word)
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

        public async Task<double?> CosineDistance(string a, string b)
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
    }
}
