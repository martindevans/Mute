using System.Threading.Tasks;

using Miki.Anilist;
using Miki.Anilist.Objects;

namespace Mute.Moe.Services.Information.Anime;

public class MikibotAnilistMangaSearch
    : BaseMikibotMediaSearchService<IManga>, IMangaInfo
{
    public MikibotAnilistMangaSearch()
        : base(MediaFormat.MOVIE, MediaFormat.MUSIC, MediaFormat.ONA, MediaFormat.ONE_SHOT, MediaFormat.OVA, MediaFormat.SPECIAL, MediaFormat.TV, MediaFormat.TV_SHORT)    //This is a list of formats _not_ to return!
    {
    }

    public Task<IManga?> GetMangaInfoAsync(string search)
    {
        return GetItemInfoAsync(search);
    }

    public IAsyncEnumerable<IManga> GetMangasInfoAsync(string search)
    {
        return GetItemsInfoAsync(search);
    }

    public IAsyncEnumerable<IManga> GetMangasInfoAsync(ICharacter search)
    {
        return GetItemsInfoAsync(search);
    }

    protected override IManga WrapItem(IMedia media)
    {
        return new MikibotManga(media);
    }

    protected override string ExtractId(IManga item)
    {
        return item.Id;
    }

    private class MikibotManga(IMedia media)
        : IManga
    {
        public string Id { get; } = media.Id.ToString();

        public string TitleEnglish { get; } = media.EnglishTitle;
        public string TitleJapanese { get; } = media.NativeTitle;
        public string Description { get; } = media.Description;

        public string Url { get; } = media.Url;
        public string ImageUrl { get; } = media.CoverImage;

        public int? Chapters { get; } = media.Chapters;
        public int? Volumes { get; } = media.Volumes;
    }
}