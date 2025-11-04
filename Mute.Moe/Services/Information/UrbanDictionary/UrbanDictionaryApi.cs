using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using FluidCaching;
using Newtonsoft.Json;

namespace Mute.Moe.Services.Information.UrbanDictionary;

public class UrbanDictionaryApi
    : IUrbanDictionary
{
    private readonly HttpClient _http;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly FluidCache<CacheEntry> _definitionCache;
    private readonly IIndex<string, CacheEntry> _definitionsByWord;

    public UrbanDictionaryApi(Configuration config,  IHttpClientFactory http)
    {
        _http = http.CreateClient();
        _definitionCache = new FluidCache<CacheEntry>(
            (int)(config.UrbanDictionary?.CacheSize ?? 128),
            TimeSpan.FromSeconds(config.UrbanDictionary?.CacheMinTimeSeconds ?? 30),
            TimeSpan.FromSeconds(config.UrbanDictionary?.CacheMaxTimeSeconds ?? 3600),
            () => DateTime.UtcNow
        );
        _definitionsByWord = _definitionCache.AddIndex("byWord", a => a.Word);
    }

    public async Task<IReadOnlyList<IUrbanDefinition>> SearchTermAsync(string term)
    {
        var urlTerm = HttpUtility.UrlEncode(term);

        //Try to get from the cache
        var item = await _definitionsByWord.GetItem(urlTerm);
        if (item != null)
            return item.Entries;

        //Get it from the API, early exit if we get nothing
        var response = await SearchTermNoCacheAsync(urlTerm);
        if (response == null)
            return [ ];

        //Add it to the cache
        _definitionCache.Add(new CacheEntry(urlTerm, response.Entries));

        //Return the result
        return response.Entries;
    }

    private async Task<CacheEntry?> SearchTermNoCacheAsync(string key)
    {
        using var httpResponse = await _http.GetAsync($"http://api.urbandictionary.com/v0/define?term={key}");
        if (!httpResponse.IsSuccessStatusCode)
            return null;

        //Parse JSON of response
        var response = JsonConvert.DeserializeObject<Response?>(await httpResponse.Content.ReadAsStringAsync());

        //If the response contains no useful data return nothing
        var items = response?.Items;
        return items == null
              ? null
              : new CacheEntry(key, items);
    }

    private class CacheEntry(string word, IReadOnlyList<IUrbanDefinition> entries)
        : IEquatable<CacheEntry>
    {
        public readonly string Word = word;
        public readonly IReadOnlyList<IUrbanDefinition> Entries = entries;

        public bool Equals(CacheEntry? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return string.Equals(Word, other.Word);
        }

        public override bool Equals(object? obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((CacheEntry)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Word);
        }

        public static bool operator ==(CacheEntry? left, CacheEntry? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(CacheEntry? left, CacheEntry? right)
        {
            return !Equals(left, right);
        }
    }

    private class Response
    {
        [JsonProperty("list"), UsedImplicitly] public List<Entry>? Items{ get; private set; }
    }

    private class Entry
        : IUrbanDefinition
    {
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable 0649 // Field not assigned
        [JsonProperty("definition"), UsedImplicitly] private string? _definition;
        [JsonProperty("permalink"), UsedImplicitly] private Uri? _permalink;
        [JsonProperty("thumbs_up"), UsedImplicitly] private int? _thumbsUp;
        [JsonProperty("thumbs_down"), UsedImplicitly] private int? _thumbsDown;
        [JsonProperty("word"), UsedImplicitly] private string? _word;
        [JsonProperty("written_on"), UsedImplicitly] private DateTime? _writtenOn;
        [JsonProperty("example"), UsedImplicitly] private string? _example;
#pragma warning restore 0649 // Field not assigned
#pragma warning restore IDE0044 // Add readonly modifier

        public string Definition => _definition ?? throw new InvalidOperationException("API returned null value for `definition` field");
        public Uri Permalink => _permalink ?? throw new InvalidOperationException("API returned null value for `permalink` field");
        public int ThumbsUp => _thumbsUp ?? throw new InvalidOperationException("API returned null value for `thumbs_up` field");
        public int ThumbsDown => _thumbsDown ?? throw new InvalidOperationException("API returned null value for `thumbs_down` field");
        public string Word => _word ?? throw new InvalidOperationException("API returned null value for `word` field");
        public DateTime WrittenOn => _writtenOn ?? throw new InvalidOperationException("API returned null value for `written_on` field");
        public string Example => _example ?? throw new InvalidOperationException("API returned null value for `example` field");
    }
}