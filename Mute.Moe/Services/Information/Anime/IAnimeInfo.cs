using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Mute.Moe.Services.Information.Anime
{
    public interface IAnimeInfo
    {
        [NotNull, ItemCanBeNull] Task<IAnime> GetAnimeInfoAsync(string search);
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
