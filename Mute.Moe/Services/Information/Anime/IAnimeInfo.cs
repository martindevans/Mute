using Mute.Moe.Tools;
using Mute.Moe.Tools.Providers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Mute.Anilist;


namespace Mute.Moe.Services.Information.Anime;

/// <summary>
/// Retrieve information about anime series
/// </summary>
public interface IAnimeInfo
{
    /// <summary>
    /// Get general information about a single anime series, movie, ONA, OVA etc, from it's title. Information includes title (english and Japanese), description, airing date and genre.
    /// </summary>
    /// <param name="title">The title of the anime</param>
    /// <returns></returns>
    Task<IAnime?> GetAnimeInfoAsync(string title);

    /// <summary>
    /// Search for anime, anime movies, ONA, OVA etc. Search string could be part of the title, a character name, or part of the description.
    /// </summary>
    /// <param name="search">The term to search for - could be part of the title, a character name or part of the description</param>
    /// <param name="limit">Maximum number of results to return</param>
    /// <returns></returns>
    IAsyncEnumerable<IAnime> GetAnimesInfoAsync(string search, int limit);

    /// <summary>
    /// Provides a list of all the anime airing in a specific season.<br />
    /// - Capability: Anime season listing.<br />
    /// - Inputs: Year and Season.<br />
    /// - Outputs: List of all anime airing in the given season.
    /// </summary>
    /// <param name="year">The year</param>
    /// <param name="season">The season (0=Winter, 1=Spring, 2=Summer, 3=Fall)</param>
    /// <returns></returns>
    IAsyncEnumerable<IAnime> GetSeasonAnimes(int year, int season);
}

/// <summary>
/// Information about an anime series
/// </summary>
public interface IAnime
{
    /// <summary>
    /// Unique ID of this series
    /// </summary>
    long Id { get; }

    /// <summary>
    /// Series title in English
    /// </summary>
    string? TitleEnglish { get; }

    /// <summary>
    /// Series title in Japanese
    /// </summary>
    string? TitleJapanese { get; }

    /// <summary>
    /// Description of the series
    /// </summary>
    string Description { get; }

    /// <summary>
    /// URL with more info about this series
    /// </summary>
    string Url { get; }

    /// <summary>
    /// Start date of broadcast
    /// </summary>
    DateTimeOffset? StartDate { get; }

    /// <summary>
    /// End date of broadcast
    /// </summary>
    DateTimeOffset? EndDate { get; }

    /// <summary>
    /// Is this an adult-only/NSFW series
    /// </summary>
    bool Adult { get; }

    /// <summary>
    /// URL for an image related to this series (e.g. banner or title splash)
    /// </summary>
    string ImageUrl { get; }

    /// <summary>
    /// List of genres of this series
    /// </summary>
    IReadOnlyList<string> Genres { get; }

    /// <summary>
    /// Total number of episodes
    /// </summary>
    uint? TotalEpisodes { get; }
}

/// <summary>
/// Provides manga related tools to LLMs
/// </summary>
public partial class AnimeToolProvider
    : IToolProvider
{
    private readonly IAnimeInfo _info;

    /// <inheritdoc />
    public IReadOnlyList<ITool> Tools { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="info"></param>
    public AnimeToolProvider(IAnimeInfo info)
    {
        _info = info;

        var tools = new List<AutoTool>
        {
            new("anime_info", false, info.GetAnimeInfoAsync),
            new("anime_search", false, GetAnimesInfoAsync, postprocess: AutoTool.AsyncEnumerableToEnumerable<IAnime>),
            new("anime_season", false, GetSeasonAnimes),
        };

        if (info is MuteAnilistInfoService anilist)
        {
            tools.Add(new("anime_manga_related_media", false, GetRelatedAnimeMedias));
        }

        Tools = tools;
    }

    /// <summary>
    /// Search for anime, anime movies, ONA, OVA etc. Search string could be part of the title, a character name, or part of the description.
    /// </summary>
    /// <param name="search">The term to search for - could be part of the title, a character name or part of the description</param>
    /// <param name="limit">Maximum number of results to return (max 16)</param>
    /// <returns></returns>
    private async Task<IAsyncEnumerable<IAnime>> GetAnimesInfoAsync(string search, int limit)
    {
        return _info.GetAnimesInfoAsync(
            search,
            Math.Min(16, limit)
        );
    }

    /// <summary>
    /// Given the ID of an anime or manga retrieve other related media (e.g. original source, sequels, prequels, spinoffs etc).<br />
    /// - Capability: Anime/Manga media relation retrieval.<br />
    /// - Inputs: Anime ID or Manga ID.<br />
    /// - Outputs: List of related media.
    /// </summary>
    /// <param name="id">ID of an anime or manga. `anime_info`/`anime_search`/`manga_info`/`mamga_search` tools can provide this.</param>
    /// <returns></returns>
    private async Task<object> GetRelatedAnimeMedias(long id)
    {
        // This tool is only available if the service is this more specific type
        var service = (MuteAnilistInfoService)_info;

        // Error if we can't find this media
        var media = await service.GetMediaInfoAsync(id);
        if (media == null)
            return new { error = $"Cannot find media with ID '{id}'" };

        // If there are no relations just return an empty array
        var edges = media.Relations?.Edges;
        if (edges == null)
            return Array.Empty<object>();

        var items = new List<object>();
        foreach (var edge in edges)
        {
            if (edge.Node == null)
                continue;

            // Skip nodes that we don't know the type of
            var node = edge.Node;
            if (node.Type is null or MediaType.Unknown)
                continue;

            // Only take certain relations
            var type = edge.RelationType;
            switch (type)
            {
                case MediaRelation.Adaptation:
                case MediaRelation.Prequel:
                case MediaRelation.Sequel:
                case MediaRelation.Parent:
                case MediaRelation.SideStory:
                case MediaRelation.Summary:
                case MediaRelation.Alternative:
                case MediaRelation.SpinOff:
                case MediaRelation.Source:
                case MediaRelation.Compilation:
                    items.Add(new
                    {
                        ID = node.Id,
                        RelationType = node.Type,
                        Title = node.Title
                    });
                    break;

                // We don't want these relation types
                case MediaRelation.Contains:
                case MediaRelation.Other:
                case MediaRelation.Character:
                default:
                    break;
            }
        }

        return items;
    }

    /// <summary>
    /// Provides a list of all the anime airing in a specific season.<br />
    /// - Capability: Anime season listing.<br />
    /// - Inputs: Year and Season.<br />
    /// - Outputs: List of all anime airing in the given season.
    /// </summary>
    /// <param name="year">The year</param>
    /// <param name="season">The season (0=Winter, 1=Spring, 2=Summer, 3=Fall)</param>
    /// <returns></returns>
    private IAsyncEnumerable<string> GetSeasonAnimes(int year, int season)
    {
        return _info
            .GetSeasonAnimes(year, season)
            .Select(a => a.TitleEnglish ?? a.TitleJapanese)
            .Where(a => a != null)
            .Select(CleanupTitle);

        static string CleanupTitle(string? title)
        {
            // Season N -> SN
            title = SeasonReplacer().Replace(title!, "S$1");

            // Part N -> PN
            title = PartReplacer().Replace(title, "P$1");

            // Trim to max length limit
            if (title.Length >= 100)
                title = $"{title[..90]}…";

            return title;
        }
    }

    [GeneratedRegex(@"\bSeason\s+(\d+)\b", RegexOptions.IgnoreCase, "en-GB")]
    private static partial Regex SeasonReplacer();

    [GeneratedRegex(@"\bPart\s+(\d+)\b", RegexOptions.IgnoreCase, "en-GB")]
    private static partial Regex PartReplacer();
}