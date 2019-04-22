using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Mute.Moe.Services.Information.Anime
{
    public interface IAnimeInfo
    {
        /// <summary>
        /// Get a single anime given the search term (best match)
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        [NotNull, ItemCanBeNull] Task<IAnime> GetAnimeInfoAsync(string search);

        /// <summary>
        /// Get all animes by the given search term
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        [NotNull, ItemNotNull] Task<IAsyncEnumerable<IAnime>> GetAnimesInfoAsync(string search);

        [NotNull, ItemNotNull] Task<IAsyncEnumerable<IAnime>> GetAnimesInfoAsync(ICharacter character);
    }

    public interface IAnime
    {
        string Id { get; }
        string TitleEnglish { get; }
        string TitleJapanese { get; }
        string Description { get; }
        string Url { get; }

        DateTimeOffset? StartDate { get; }
        DateTimeOffset? EndDate { get; }
        
        bool Adult { get; }

        string ImageUrl { get; }

        IReadOnlyList<string> Genres { get; }

        uint? TotalEpisodes { get; }
    }
}
