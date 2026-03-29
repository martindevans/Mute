using Mute.BraveSearch;
using System.Threading.Tasks;
using Mute.BraveSearch.Models;

namespace Mute.Moe.Tools.Providers;

/// <summary>
/// Provides web search through the brave API
/// </summary>
public class BraveWebSearchProvider
    : IToolProvider
{
    private readonly IBraveSearchClient _client;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="client"></param>
    public BraveWebSearchProvider(IBraveSearchClient client)
    {
        _client = client;

        Tools = [ 
            new AutoTool("web_search_news", true, NewsSearch),
        ];
    }

    /// <inheritdoc />
    public IReadOnlyList<ITool> Tools { get; }

    /// <summary>
    /// Search the web for recent news.<br />
    /// - Capability: Web news search.<br />
    /// - Inputs: Query for information.<br />
    /// - Outputs: A list news item summaries from the search engine.
    /// </summary>
    /// <param name="query">Web search query.</param>
    /// <returns></returns>
#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
    private async Task<NewsItem[]> NewsSearch(ITool.CallContext callCtx, string query)
#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
    {
        var results = await _client.NewsSearchAsync(new NewsSearchRequest(query)
        {
            Country = "GB",
            ExtraSnippets = true,
        });

        return results
            .Results
            .Select(NewsItem.Create)
            .Where(a => a != null)
            .Take(20)
            .Cast<NewsItem>()
            .ToArray();
    }

    /// <summary>
    /// A news item retrieved from a search
    /// </summary>
    /// <param name="Title"></param>
    /// <param name="Description"></param>
    /// <param name="Age"></param>
    /// <param name="Source"></param>
    /// <param name="Snippets"></param>
    [UsedImplicitly]
    public record NewsItem(string Title, string Description, string Age, string Source, IReadOnlyList<string> Snippets)
    {
        internal static NewsItem? Create(NewsResult result)
        {
            var desc = result.Description;
            if (string.IsNullOrWhiteSpace(desc))
                return null;

            var age = result.Age;
            if (string.IsNullOrWhiteSpace(age))
                return null;

            var source = result.Profile?.Name ?? result.Profile?.LongName;
            if (string.IsNullOrWhiteSpace(source))
                return null;

            return new NewsItem(
                result.Title,
                desc,
                age,
                source,
                result.ExtraSnippets ?? []
            );
        }
    }
}