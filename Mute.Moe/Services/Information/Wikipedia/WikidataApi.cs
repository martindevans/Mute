using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mute.Moe.Utilities;
using Newtonsoft.Json;

namespace Mute.Moe.Services.Information.Wikipedia
{
    public class WikidataApi
    {
        private readonly IHttpClient _client;

        public WikidataApi(IHttpClient client)
        {
            _client = client;
        }

        public async Task<WikidataResponseContainer> SearchData(string topic)
        {
            var escTopic = Uri.EscapeUriString(topic);
            using (var httpResponse = await _client.GetAsync($"https://www.wikidata.org/w/api.php?action=wbsearchentities&utf8=1&format=json&language=en&type=item&continue=0&search={escTopic}"))
            {
                if (!httpResponse.IsSuccessStatusCode)
                    return null;

                try
                {
                    var response = await httpResponse.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<WikidataResponseContainer>(response);
                }
                catch (JsonException)
                {
                    return null;
                }
            }
        }

        #pragma warning disable CS0649
        public class WikidataResponseContainer
        {
            [JsonProperty("searchinfo")] public WikidataSearchInfo SearchInfo;
            [JsonProperty("search")] public List<WikidataSearchResponseItem> Search;
        }

        public class WikidataSearchInfo
        {
            [JsonProperty("search")] public string Search;
        }

        public class WikidataSearchResponseItem
        {
            [JsonProperty("repository")] public string Repository;
            [JsonProperty("id")] public string Id;
            [JsonProperty("concepturi")] public string ConceptUri;
            [JsonProperty("title")] public string Title;
            [JsonProperty("pageid")] public string PageId;
            [JsonProperty("uri")] public string Uri;
            [JsonProperty("label")] public string Label;
            [JsonProperty("description")] public string Description;
        }

        public class WikidataSearchResponseItemMatch
        {
            [JsonProperty("type")] public string Type;
            [JsonProperty("language")] public string Language;
            [JsonProperty("text")] public string Text;
        }
        #pragma warning restore CS0649
    }
}
