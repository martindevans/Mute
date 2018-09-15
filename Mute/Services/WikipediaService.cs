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

        [ItemCanBeNull] public async Task<string> GetDefinition(string topic, int length = 3)
        {
            var escTopic = Uri.EscapeUriString(topic);
            var httpResponse = await _client.GetAsync($"https://en.wikipedia.org/w/api.php?format=json&action=query&prop=extracts&exintro&explaintext&redirects=1&titles={escTopic}&exsentences={length}");

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
