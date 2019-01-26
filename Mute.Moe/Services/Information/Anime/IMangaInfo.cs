using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Mute.Moe.Services.Information.Anime
{
    public interface IMangaInfo
    {
        [ItemCanBeNull] Task<IManga> GetMangaInfoAsync(string search);
    }

    public interface IManga
    {
        string Id { get; }

        string TitleEnglish { get; }
        string TitleJapanese { get; }

        string Description { get; }

        string Url { get; }

        int? Chapters { get; }
        int? Volumes { get; }

        string ImageUrl { get; }
    }
}
