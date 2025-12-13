using Mute.Moe.Tools;
using Mute.Moe.Tools.Providers;
using System.Threading.Tasks;


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

public interface IAnime
{
    string Id { get; }
    string? TitleEnglish { get; }
    string? TitleJapanese { get; }
    string Description { get; }
    string Url { get; }

    DateTimeOffset? StartDate { get; }
    DateTimeOffset? EndDate { get; }
        
    bool Adult { get; }

    string ImageUrl { get; }

    IReadOnlyList<string> Genres { get; }

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