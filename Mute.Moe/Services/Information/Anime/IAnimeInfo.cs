using Mute.Moe.Tools;
using System.Threading.Tasks;


namespace Mute.Moe.Services.Information.Anime;

/// <summary>
/// Retrieve information about anime series
/// </summary>
public interface IAnimeInfo
{
    /// <summary>
    /// Get information about a single anime
    /// </summary>
    /// <param name="title">The title of the anime</param>
    /// <returns></returns>
    Task<IAnime?> GetAnimeInfoAsync(string title);

    /// <summary>
    /// Search for anime
    /// </summary>
    /// <param name="search">The term to search for</param>
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
            new AutoTool("anime_search", false, info.GetAnimesInfoAsync, postprocessAsync: async x => await ((IAsyncEnumerable<IAnime>)x!).Take(1024).ToArrayAsync()),
        ];
    }
}