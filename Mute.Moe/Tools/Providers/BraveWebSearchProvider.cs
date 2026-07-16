using Mute.BraveSearch;
using Mute.BraveSearch.Models;
using System.Threading.Tasks;
using Discord;
using HandyAgentFramework;

namespace Mute.Moe.Tools.Providers;

/// <summary>
/// Provides web search through the brave API
/// </summary>
public class BraveWebSearchProvider
    : IToolProvider
{
    private readonly IBraveSearchClient _client;
    private readonly IDiscordClient _discord;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="client"></param>
    /// <param name="discord"></param>
    public BraveWebSearchProvider(IBraveSearchClient client, IDiscordClient discord)
    {
        _client = client;
        _discord = discord;

        Tools =
        [
            new DocStringTool(ToolGroups.Info.WebSearch, "search_news", NewsSearch),
            
            //todo: disabled for now, need to investigate filtering or white/blacklisting of URLs
            //new DocStringTool(ToolGroups.Info.WebSearch, "search_web", WebSearch),
        ];
    }

    /// <inheritdoc />
    public IReadOnlyList<ToolDefinition> Tools { get; }

    #region news
    /// <summary>
    /// Search the web for recent news.<br />
    /// Use quotes for exact phrase matching e.g. "climate change"<br />
    /// Exclude terms with minus e.g. technology -cryptocurrency<br />
    /// - Capability: Web news search.<br />
    /// - Inputs: Query for information.<br />
    /// - Outputs: A list news item summaries from the search engine.
    /// </summary>
    /// <param name="query">Web news search query.</param>
    /// <returns></returns>
#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
    private async Task<NewsItem[]> NewsSearch(string query)
#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
    {
        // Check if we show allow NSFW results in the search
        var context = await MuteAgentContext.GetContext(_discord);
        var nsfw = context?.IsNsfw ?? false;
        
        // Run query
        var results = await _client.NewsSearchAsync(new NewsSearchRequest(query)
        {
            ExtraSnippets = true,
            SafeSearch = nsfw ? SafeSearch.Off : SafeSearch.Moderate,
        });

        // Extract results
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
    /// <param name="Caution">An optional note, providing a cautionary warning about the reliability of this news item</param>
    [UsedImplicitly]
    public record NewsItem(string Title, string Description, string Age, string Source, IReadOnlyList<string> Snippets, string? Caution)
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

            var caution = default(string);
            if (IsNearAprilFirst())
                caution = $"The current date is {DateOnly.FromDateTime(DateTime.UtcNow):d} - April fools **may** apply to some or all items!";

            return new NewsItem(
                result.Title,
                desc,
                age,
                source,
                result.ExtraSnippets ?? [ ],
                caution
            );
        }
    }

    private static bool IsNearAprilFirst()
    {
        var date = DateTime.UtcNow;

        var apr1st = new DateTime(date.Year, 4, 1);

        var start = apr1st - TimeSpan.FromHours(12);
        var end = apr1st + TimeSpan.FromHours(36);

        return date > start && date < end;
    }
    #endregion

    #region web
    /// <summary>
    /// General purpose web search.<br />
    /// Use quotes for exact phrase matching e.g. "climate change"<br />
    /// Exclude terms with minus e.g. technology -cryptocurrency<br />
    /// - Capability: General web search.<br />
    /// - Inputs: Query for information.<br />
    /// - Outputs: A list results with summaries from the search engine.
    /// </summary>
    /// <param name="query">Web search query.</param>
    /// <param name="maxAgeDays">Maximum age of search results (in days). Specify -1 for no age limit.</param>
    /// <returns></returns>
    #pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
    private async Task<WebItem[]> WebSearch(string query, int maxAgeDays)
#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
    {
        // Check if we show allow NSFW results in the search
        var context = await MuteAgentContext.GetContext(_discord);
        var nsfw = context?.IsNsfw ?? false;

        // Age limit
        var freshness = default(SearchFreshness);
        if (maxAgeDays >= 0)
        {
            var now = DateTime.UtcNow;
            freshness = SearchFreshness.Range(
                DateOnly.FromDateTime(now - TimeSpan.FromDays(maxAgeDays)),
                DateOnly.FromDateTime(now)
            );
        }
        
        // Run query
        var results = await _client.SearchAsync(new SearchRequest(query, ResultType.Web)
        {
            ExtraSnippets = true,
            SafeSearch = nsfw ? SafeSearch.Off : SafeSearch.Moderate,
            Freshness = freshness
        });

        // Extract results
        return results
              .Web?
              .Results
              .Select(WebItem.Create)
              .Where(a => a != null)
              .Take(20)
              .Cast<WebItem>()
              .ToArray() ?? [ ];
    }

    /// <summary>
    /// A web item retrieved from a search
    /// </summary>
    /// <param name="Title"></param>
    /// <param name="Description"></param>
    /// <param name="Age"></param>
    /// <param name="Nsfw"></param>
    /// <param name="Source"></param>
    /// <param name="Snippets"></param>
    [UsedImplicitly]
    public record WebItem(string Title, string Description, string Age, bool Nsfw, string Source, IReadOnlyList<string> Snippets)
    {
        internal static WebItem? Create(WebResult result)
        {
            var desc = result.Description;
            if (string.IsNullOrWhiteSpace(desc))
                return null;

            var age = result.Age;
            if (string.IsNullOrWhiteSpace(age))
                return null;

            var nsfw = !result.FamilyFriendly;
            
            return new WebItem(
                result.Title,
                desc,
                age,
                nsfw,
                result.Url,
                result.ExtraSnippets ?? [ ]
            );
        }
    }
    #endregion
}