using System.Threading.Tasks;

namespace Mute.Moe.Services.Information.Anime;

public interface IMangaInfo
{
    Task<IManga?> GetMangaInfoAsync(string search);

    IAsyncEnumerable<IManga> GetMangasInfoAsync(string search);

    IAsyncEnumerable<IManga> GetMangasInfoAsync(ICharacter character);
}

public interface IManga
{
    string Id { get; }

    string? TitleEnglish { get; }
    string? TitleJapanese { get; }

    string Description { get; }

    string Url { get; }

    int? Chapters { get; }
    int? Volumes { get; }

    string ImageUrl { get; }
}