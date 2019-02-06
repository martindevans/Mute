using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluidCaching;
using JetBrains.Annotations;
using Mute.Moe.Utilities;
using Newtonsoft.Json;

namespace Mute.Moe.Services.Information.Wikipedia
{
    public class WikipediaApi
        : IWikipedia
    {
        private readonly IHttpClient _client;

        private readonly FluidCache<Tuple<string, IReadOnlyList<IDefinition>>> _cache;
        private readonly IIndex<string, Tuple<string, IReadOnlyList<IDefinition>>> _bySearchTerm;

        public WikipediaApi(IHttpClient client)
        {
            _client = client;

            _cache = new FluidCache<Tuple<string, IReadOnlyList<IDefinition>>>(1024, TimeSpan.FromMinutes(5), TimeSpan.FromDays(1), () => DateTime.UtcNow);
            _bySearchTerm = _cache.AddIndex("bySearchTerm", a => a.Item1);
        }

        [ItemCanBeNull] public async Task<IReadOnlyList<IDefinition>> Define([NotNull] string topic, int length = 3)
        {
            var escTopic = Uri.EscapeUriString(topic.ToLowerInvariant());

            //Get data about this topic from cache
            var item = await _bySearchTerm.GetItem(escTopic);
            if (item != null)
                return item.Item2;

            //Download it and add to cache
            var def = await FetchDefinitionAsync(escTopic, length);
            _cache.Add(Tuple.Create(escTopic, def));

            return def;
        }

        [ItemNotNull]
        private async Task<IReadOnlyList<IDefinition>> FetchDefinitionAsync([NotNull] string escapedTopic, int length)
        {
            using (var httpResponse = await _client.GetAsync($"https://en.wikipedia.org/w/api.php?format=json&action=query&prop=extracts&exintro&explaintext&redirects=1&titles={escapedTopic}&exsentences={length}"))
            {
                if (!httpResponse.IsSuccessStatusCode)
                    return Array.Empty<IDefinition>();

                //Parse JSON of response
                var response = JsonConvert.DeserializeObject<DefinitionResponseContainer>(await httpResponse.Content.ReadAsStringAsync());

                //If the response contains no useful data return nothing
                var pages = response?.Query?.Pages;
                if (pages == null)
                    return Array.Empty<IDefinition>();

                //Extract all the valid items we can from the response
                var result = new List<IDefinition>();
                foreach (var (_, value) in pages)
                {
                    if (value.Title == null || value.Extract == null)
                        continue;
                    if (!ulong.TryParse(value.PageId, out var id))
                        continue;

                    result.Add(new WikipediaApiDefinition(value.Title, id, value.Extract));
                }

                return result;
            }
        }

        private class WikipediaApiDefinition
            : IDefinition
        {
            public string Title { get; }
            public string Definition { get; }

            private ulong PageId { get; }
            [NotNull] public string Url => $"https://en.wikipedia.org/?curid={PageId}";

            public WikipediaApiDefinition([NotNull] string title, ulong pageId, [NotNull] string definition)
            {
                Title = title;
                PageId = pageId;
                Definition = definition;
            }
        }

        #pragma warning disable CS0649
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
            [JsonProperty("pageid")] public string PageId;
            [JsonProperty("title")] public string Title;
            [JsonProperty("extract")] public string Extract;
        }
        #pragma warning restore CS0649
    }
}
