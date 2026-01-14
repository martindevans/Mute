using Mute.Moe.Tools;
using Mute.Moe.Tools.Providers;
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
public class AnimeToolProvider
    : IToolProvider
{
    /// <inheritdoc />
    public IReadOnlyList<ITool> Tools { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="info"></param>
    public AnimeToolProvider(IAnimeInfo info)
    {
        Tools =
        [
            new AutoTool("anime_info", false, info.GetAnimeInfoAsync),
            new AutoTool("anime_search", false, info.GetAnimesInfoAsync, postprocess: AutoTool.AsyncEnumerableToEnumerable<IAnime>),
        ];
    }
}