using System.Threading.Tasks;
using Mute.Moe.Tools;
using Mute.Moe.Tools.Providers;

namespace Mute.Moe.Services.Information.Anime;

/// <summary>
/// Retrieve information about manga series
/// </summary>
public interface IMangaInfo
{
    /// <summary>
    /// Get information about a single manga, graphic novel or light novel. Information includes title (english and Japanese), description chapters and volumes count.
    /// </summary>
    /// <param name="title">The title of the manga</param>
    /// <returns></returns>
    Task<IManga?> GetMangaInfoAsync(string title);

    /// <summary>
    /// Fuzzy search for anime, graphic novel or light novel.
    /// </summary>
    /// <param name="search">The term to search for</param>
    /// <param name="limit">Maximum number of results to return</param>
    /// <returns></returns>
    IAsyncEnumerable<IManga> GetMangasInfoAsync(string search, int limit);
}

/// <summary>
/// Information about a manga series
/// </summary>
public interface IManga
{
    /// <summary>
    /// Unique ID of this manga
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Title in english
    /// </summary>
    string? TitleEnglish { get; }

    /// <summary>
    /// Title in Japanese
    /// </summary>
    string? TitleJapanese { get; }

    /// <summary>
    /// Description of this manga
    /// </summary>
    string Description { get; }

    /// <summary>
    /// URL with more information about this manga
    /// </summary>
    string Url { get; }

    /// <summary>
    /// Number of chapters
    /// </summary>
    int? Chapters { get; }

    /// <summary>
    /// Number of volumes
    /// </summary>
    int? Volumes { get; }

    /// <summary>
    /// URL with an image for this manga (e.g. book cover)
    /// </summary>
    string ImageUrl { get; }
}

/// <summary>
/// Provides manga related tools to LLMs
/// </summary>
public class MangaToolProvider
    : IToolProvider
{
    /// <inheritdoc />
    public IReadOnlyList<ITool> Tools { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="info"></param>
    public MangaToolProvider(IMangaInfo info)
    {
        Tools =
        [
            new AutoTool("manga_info", false, info.GetMangaInfoAsync),
            new AutoTool("manga_search", false, info.GetMangasInfoAsync, postprocess: AutoTool.AsyncEnumerableToEnumerable<IAnime>),
        ];
    }
}