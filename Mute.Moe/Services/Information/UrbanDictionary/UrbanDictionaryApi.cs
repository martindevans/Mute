using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using FluidCaching;
using JetBrains.Annotations;
using Mute.Moe.Utilities;
using Newtonsoft.Json;

namespace Mute.Moe.Services.Information.UrbanDictionary
{
    public class UrbanDictionaryApi
        : IUrbanDictionary
    {
        [NotNull] private readonly IHttpClient _http;

        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly FluidCache<CacheEntry> _definitionCache;
        private readonly IIndex<string, CacheEntry> _definitionsByWord;

        public UrbanDictionaryApi([NotNull] Configuration config, [NotNull] IHttpClient http)
        {
            _http = http;
            _definitionCache = new FluidCache<CacheEntry>(
                (int)config.UrbanDictionary.CacheSize,
                TimeSpan.FromSeconds(config.UrbanDictionary.CacheMinTimeSeconds),
                TimeSpan.FromSeconds(config.UrbanDictionary.CacheMaxTimeSeconds),
                () => DateTime.UtcNow
            );
            _definitionsByWord = _definitionCache.AddIndex("byWord", a => a.Word);
        }

        [NotNull, ItemNotNull] public async Task<IReadOnlyList<IUrbanDefinition>> SearchTermAsync(string term)
        {
            //Sanity check term encodes into URL form
            var urlTerm = HttpUtility.UrlEncode(term);
            if (urlTerm == null)
                return Array.Empty<IUrbanDefinition>();

            //Try to get from the cache
            var item = await _definitionsByWord.GetItem(urlTerm);
            if (item != null)
                return item.Entries;

            //Get it from the API, early exit if we get nothing
            var response = await SearchTermNoCacheAsync(urlTerm);
            if (response == null)
                return Array.Empty<IUrbanDefinition>();

            //Add it to the cache
            _definitionCache.Add(new CacheEntry(urlTerm, response.Entries));

            //Return the result
            return response.Entries;
        }

        [NotNull, ItemCanBeNull] private async Task<CacheEntry> SearchTermNoCacheAsync(string key)
        {
            using (var httpResponse = await _http.GetAsync($"http://api.urbandictionary.com/v0/define?term={key}"))
            {
                if (!httpResponse.IsSuccessStatusCode)
                    return null;

                //Parse JSON of response
                var response = JsonConvert.DeserializeObject<Response>(await httpResponse.Content.ReadAsStringAsync());

                //If the response contains no useful data return nothing
                var items = response?.Items;
                if (items == null)
                    return null;

                return new CacheEntry(key, items);
            }
        }

        private class CacheEntry
            : IEquatable<CacheEntry>
        {
            [NotNull] public readonly string Word;
            [NotNull] public readonly IReadOnlyList<IUrbanDefinition> Entries;

            public CacheEntry([NotNull] string word, [NotNull] IReadOnlyList<IUrbanDefinition> entries)
            {
                Word = word;
                Entries = entries;
            }

            public bool Equals([CanBeNull] CacheEntry other)
            {
                if (ReferenceEquals(null, other))
                    return false;
                if (ReferenceEquals(this, other))
                    return true;
                return string.Equals(Word, other.Word);
            }

            public override bool Equals([CanBeNull] object obj)
            {
                if (ReferenceEquals(null, obj))
                    return false;
                if (ReferenceEquals(this, obj))
                    return true;
                if (obj.GetType() != GetType())
                    return false;
                return Equals((CacheEntry)obj);
            }

            public override int GetHashCode()
            {
                return Word.GetHashCode();
            }

            public static bool operator ==([CanBeNull] CacheEntry left, [CanBeNull] CacheEntry right)
            {
                return Equals(left, right);
            }

            public static bool operator !=([CanBeNull] CacheEntry left, [CanBeNull] CacheEntry right)
            {
                return !Equals(left, right);
            }
        }

        private class Response
        {
            [JsonProperty("list")] public List<Entry> Items;
        }

        private class Entry
            : IUrbanDefinition
        {
            [JsonProperty("definition")] private string _definition;
            public string Definition => _definition;

            [JsonProperty("permalink")] private Uri _permalink;
            public Uri Permalink => _permalink;

            [JsonProperty("thumbs_up")] private int _thumbsUp;
            public int ThumbsUp => _thumbsUp;

            [JsonProperty("thumbs_down")] private int _thumbsDown;
            public int ThumbsDown => _thumbsDown;

            [JsonProperty("sound_urls")] private Uri[] _sounds;
            public IReadOnlyList<Uri> Sounds => _sounds;

            [JsonProperty("word")] private string _word;
            public string Word => _word;

            [JsonProperty("written_on")] private DateTime _writtenOn;
            public DateTime WrittenOn => _writtenOn;

            [JsonProperty("example")] private string _example;
            public string Example => _example;
        }
    }
}
