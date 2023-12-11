using System.Net.Http;
using System.Threading.Tasks;
using FluidCaching;

using Newtonsoft.Json;

namespace Mute.Moe.Services.Information.Wikipedia;

public class WikipediaApi
    : IWikipedia
{
    private readonly HttpClient _client;

    private readonly FluidCache<Tuple<string, IReadOnlyList<IDefinition>>> _cache;
    private readonly IIndex<string, Tuple<string, IReadOnlyList<IDefinition>>> _bySearchTerm;

    public WikipediaApi(IHttpClientFactory client)
    {
        _client = client.CreateClient();

        _cache = new FluidCache<Tuple<string, IReadOnlyList<IDefinition>>>(1024, TimeSpan.FromMinutes(5), TimeSpan.FromDays(1), () => DateTime.UtcNow);
        _bySearchTerm = _cache.AddIndex("bySearchTerm", a => a.Item1);
    }

    public async Task<IReadOnlyList<IDefinition>> Define(string? topic, int length = 3)
    {
        if (topic == null)
            return Array.Empty<IDefinition>();

        var key = topic + length;

        // Get data about this topic from cache
        var item = await _bySearchTerm.GetItem(key);
        if (item != null)
            return item.Item2;

        // Download it and add to cache
        var def = await FetchDefinitionAsync(topic, length);
        _cache.Add(Tuple.Create(key, def));

        return def;
    }

    private async Task<IReadOnlyList<IDefinition>> FetchDefinitionAsync(string escapedTopic, int length)
    {
        async Task<IReadOnlyList<IDefinition>> GetPageDefinitions(PropPage page)
        {
            if (page.Categories != null && page.Categories.Any(a => (a.Title?.Equals("Category:All disambiguation pages") ?? false) || (a.Title?.Equals("Category:Disambiguation pages") ?? false)))
            {
                // This is a disambiguation page, get all links on the page and fetch those definitions instead
                using var httpResponse = await _client.GetAsync($"https://en.wikipedia.org/w/api.php?format=json&action=query&redirects=1&titles={escapedTopic}&prop=links");
                if (!httpResponse.IsSuccessStatusCode)
                    return Array.Empty<IDefinition>();

                //Parse JSON of response
                var response = JsonConvert.DeserializeObject<PropResponseContainer?>(await httpResponse.Content.ReadAsStringAsync());

                // Find all links
                var links = response?.Query?.Pages?.Select(p => p.Value).SelectMany(a => a.Links ?? Array.Empty<Property>());
                if (links == null)
                    return Array.Empty<IDefinition>();

                // Fetch all the links
                var results = new List<IDefinition>();
                foreach (var link in links.Where(a => !string.IsNullOrWhiteSpace(a.Title)))
                    results.AddRange(await Define(link.Title, 1));

                return results;
            }
            else
            {
                // This is an actual page, fetch the definition
                using var httpResponse = await _client.GetAsync($"https://en.wikipedia.org/w/api.php?format=json&action=query&prop=extracts&exintro&explaintext&redirects=1&titles={page.Title}&exsentences={length}");
                if (!httpResponse.IsSuccessStatusCode)
                    return Array.Empty<IDefinition>();

                //Parse JSON of response
                var response = JsonConvert.DeserializeObject<DefinitionResponseContainer?>(await httpResponse.Content.ReadAsStringAsync());

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

        // Find the page and get the categories for it, this way we can tell if it's a disambiguation page
        using (var httpResponse = await _client.GetAsync($"https://en.wikipedia.org/w/api.php?format=json&action=query&redirects=1&titles={escapedTopic}&prop=categories"))
        {
            if (!httpResponse.IsSuccessStatusCode)
                return Array.Empty<IDefinition>();

            //Parse JSON of response
            var response = JsonConvert.DeserializeObject<PropResponseContainer?>(await httpResponse.Content.ReadAsStringAsync());

            // Early exit if no pages were found
            var pages = response?.Query?.Pages;
            if (pages == null)
                return Array.Empty<IDefinition>();

            // Start tasks to get definitions
            var definitionsTasks = pages
                .Select(a => Task.Run(() => GetPageDefinitions(a.Value)))
                .ToArray();

            // Wait on all the tasks and save them in a list
            var definitions = new List<IDefinition>();
            foreach (var definitionsTask in definitionsTasks)
                definitions.AddRange(await definitionsTask);

            return definitions;
        }
    }

    private record WikipediaApiDefinition(string Title, ulong PageId, string Definition)
        : IDefinition
    {
        public string Url => $"https://en.wikipedia.org/?curid={PageId}";
    }

    #pragma warning disable CS0649
    private class DefinitionResponseContainer
    {
        [JsonProperty("query")] public DefinitionResponse? Query;
    }

    private class DefinitionResponse
    {
        [JsonProperty("pages")] public Dictionary<string, DefinitionPage>? Pages;
    }

    private class DefinitionPage
    {
        [JsonProperty("pageid")] public string? PageId;
        [JsonProperty("title")] public string? Title;
        [JsonProperty("extract")] public string? Extract;
    }

    private class PropResponseContainer
    {
        [JsonProperty("query")] public PropResponse? Query;
    }

    private class PropResponse
    {
        [JsonProperty("pages")] public Dictionary<string, PropPage>? Pages;
    }

    private class PropPage
    {
        [JsonProperty("pageid")] public string? PageId;
        [JsonProperty("title")] public string? Title;

        [JsonProperty("categories")] public Property[]? Categories;
        [JsonProperty("links")] public Property[]? Links;
    }

    private class Property
    {
        [JsonProperty("title")] public string? Title;
    }
    #pragma warning restore CS0649
}