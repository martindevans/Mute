using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Mute.Moe.Services.Information.Anime;

public interface IAnimeInfo
{
    /// <summary>
    /// Get a single anime given the search term (best match)
    /// </summary>
    /// <param name="search"></param>
    /// <returns></returns>
    Task<IAnime?> GetAnimeInfoAsync(string search);

    /// <summary>
    /// Get anime by the given search term
    /// </summary>
    /// <param name="search"></param>
    /// <returns></returns>
    IAsyncEnumerable<IAnime> GetAnimesInfoAsync(string search);

    /// <summary>
    /// Get all the anime featuring the given character
    /// </summary>
    /// <param name="character"></param>
    /// <returns></returns>
    IAsyncEnumerable<IAnime> GetAnimesInfoAsync(ICharacter character);
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