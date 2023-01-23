using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using FluidCaching;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Mute.Moe.Services.Words;

public class HttpWordVectors
    : IWords
{
    private readonly HttpClient _client;

    private readonly string _baseUrl;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly FluidCache<WordVector> _vectorCache;
    private readonly IIndex<string, WordVector> _indexByWord;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly FluidCache<SimilarResult> _similarCache;
    private readonly IIndex<string, SimilarResult> _indexSimilarByWord;

    private readonly Backoff _backoff = new();

    public HttpWordVectors(Configuration config, IHttpClientFactory client)
    {
        _client = client.CreateClient();
        _client.Timeout = TimeSpan.FromMilliseconds(125);

        _baseUrl = config.WordVectors?.WordVectorsBaseUrl ?? throw new ArgumentNullException(nameof(config.WordVectors.WordVectorsBaseUrl));

        _vectorCache = new FluidCache<WordVector>((int)config.WordVectors.CacheSize, TimeSpan.FromSeconds(config.WordVectors.CacheMinTimeSeconds), TimeSpan.FromMinutes(config.WordVectors.CacheMaxTimeSeconds), () => DateTime.UtcNow);
        _indexByWord = _vectorCache.AddIndex("byWord", a => a.Word);

        _similarCache = new FluidCache<SimilarResult>((int)config.WordVectors.CacheSize, TimeSpan.FromSeconds(config.WordVectors.CacheMinTimeSeconds), TimeSpan.FromMinutes(config.WordVectors.CacheMaxTimeSeconds), () => DateTime.UtcNow);
        _indexSimilarByWord = _similarCache.AddIndex("byWord", a => a.Root);
    }

    public async Task<IReadOnlyList<float>?> Vector(string word)
    {
        return (await GetVectorObject(word))?.Vector;
    }

    private async Task<WordVector?> GetVectorObject(string word)
    {
        word = word.ToLowerInvariant();

        var item = await _indexByWord.GetItem(word, GetVectorNonCached!);
        return item;
    }

    private async Task<WordVector?> GetVectorNonCached(string word)
    {
        if (!_backoff.MayTry())
            return null;

        try
        {
            var url = new UriBuilder(_baseUrl) {Path = $"get_vector/{Uri.EscapeDataString(word)}"};

            using var resp = await _client.GetAsync(url.ToString());
            if (!resp.IsSuccessStatusCode)
            {
                _backoff.Fail();
                return null;
            }

            var arr = JArray.Parse(await resp.Content.ReadAsStringAsync());

            var vector = new List<float>(300);
            foreach (var jToken in arr)
                vector.Add((float)jToken);

            _backoff.Success();
            return new WordVector(word, vector);
        }
        catch (Exception)
        {
            _backoff.Fail();
            throw;
        }
    }

    public async Task<IReadOnlyList<ISimilarWord>?> Similar(string word)
    {
        return (await GetSimilarObject(word))?.Similar;
    }

    private async Task<SimilarResult?> GetSimilarObject(string word)
    {
        word = word.ToLowerInvariant();

        var item = await _indexSimilarByWord.GetItem(word, GetSimilarNonCached!);
        return item;
    }

    private async Task<SimilarResult?> GetSimilarNonCached(string word)
    {
        if (!_backoff.MayTry())
            return null;

        try
        {
            var url = new UriBuilder(_baseUrl) {Path = $"get_similar/{Uri.EscapeDataString(word)}"};

            using var resp = await _client.GetAsync(url.ToString());
            if (!resp.IsSuccessStatusCode)
            {
                _backoff.Fail();
                return null;
            }

            SimilarWord[]? results;
            var serializer = new JsonSerializer();
            using (var sr = new StreamReader(await resp.Content.ReadAsStreamAsync()))
            using (var jsonTextReader = new JsonTextReader(sr))
                results = serializer.Deserialize<SimilarWord[]>(jsonTextReader);
            if (results == null)
                return null;

            _backoff.Success();
            return new SimilarResult(word, results);
        }
        catch (Exception)
        {
            _backoff.Fail();
            throw;
        }
    }

    public async Task<double?> Similarity(string a, string b)
    {
        var av = await GetVectorObject(a);
        var bv = await GetVectorObject(b);

        if (av == null || bv == null)
            return null;

        var acc = 0f;
        for (var i = 0; i < av.Vector.Count; i++)
            acc += MathF.Pow(av.Vector[i] - bv.Vector[i], 2);
        acc = MathF.Sqrt(acc);

        return acc;
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
#pragma warning disable IDE0044 // Add readonly modifier
        [UsedImplicitly, JsonProperty("word")] private string _word;
        // ReSharper disable once ConvertToAutoProperty
        public string Word => _word;

        [UsedImplicitly, JsonProperty("distance")] private float _distance;
#pragma warning restore IDE0044 // Add readonly modifier

        public float Similarity => 1 - Math.Max(0f, _distance);

        public SimilarWord(string word, float difference)
        {
            _word = word;
            _distance = difference;
        }
    }

    private class Backoff
    {
        private readonly object _lock = new();

        private int _failures;
        private DateTime _previousFailure;

        public void Fail()
        {
            lock (_lock)
            {
                _failures = Math.Max(_failures * 2, 1);
                _previousFailure = DateTime.UtcNow;
            }
        }

        public void Success()
        {
            lock (_lock)
            {
                lock (_lock)
                {
                    _failures = Math.Max(0, _failures - 1);
                    _previousFailure = DateTime.UtcNow;
                }
            }
        }

        public bool MayTry()
        {
            lock (_lock)
            {
                if (_failures <= 5)
                    return true;

                // How long between attempts should we wait?
                var time = Math.Min(5000, _failures * 3);
                return DateTime.UtcNow - TimeSpan.FromMilliseconds(time) > _previousFailure;
            }
        }
    }
}