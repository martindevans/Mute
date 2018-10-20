using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Mute.Services
{
    public class WikipediaService
    {
        private readonly IHttpClient _client;

        public WikipediaService(IHttpClient client)
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

        [ItemCanBeNull] public async Task<string> GetDefinition(string topic, int length = 3)
        {
            var escTopic = Uri.EscapeUriString(topic);
            using (var httpResponse = await _client.GetAsync($"https://en.wikipedia.org/w/api.php?format=json&action=query&prop=extracts&exintro&explaintext&redirects=1&titles={escTopic}&exsentences={length}"))
            {
                if (!httpResponse.IsSuccessStatusCode)
                    return null;

                try
                {
                    var response = JsonConvert.DeserializeObject<DefinitionResponseContainer>(await httpResponse.Content.ReadAsStringAsync());
                    return response?.Query?.Pages?.FirstOrDefault().Value?.Extract;
                }
                catch (JsonException)
                {
                    return null;
                }
            }
        }

        private class DefinitionResponseContainer
        {
            [JsonProperty("query")] public DefinitionResponse Query;
        }

        private class DefinitionResponse
        {
            [JsonProperty("pages")] public Dictionary<string, Page> Pages;
        }

        private class Page
        {
            [JsonProperty("pages")] public string PageId;
            [JsonProperty("title")] public string Title;
            [JsonProperty("extract")] public string Extract;
        }
    }
}
