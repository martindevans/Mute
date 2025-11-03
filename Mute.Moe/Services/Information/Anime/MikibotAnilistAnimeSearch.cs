using System.Reflection;
using System.Threading.Tasks;
using Miki.Anilist;
using Miki.Anilist.Objects;

namespace Mute.Moe.Services.Information.Anime;

/// <summary>
/// Provides anime info using AniList API
/// </summary>
public class MikibotAnilistAnimeSearch
    : BaseMikibotMediaSearchService<IAnime>, IAnimeInfo
{
    /// <summary>
    /// 
    /// </summary>
    public MikibotAnilistAnimeSearch()
        : base(MediaFormat.MANGA, MediaFormat.NOVEL, MediaFormat.MUSIC)    //This is a list of formats _not_ to return!
    {
    }

    /// <inheritdoc />
    public Task<IAnime?> GetAnimeInfoAsync(string title)
    {
        return GetItemInfoAsync(title);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<IAnime> GetAnimesInfoAsync(string search, int limit)
    {
        return GetItemsInfoAsync(search).Take(limit);
    }

    /// <inheritdoc />
    protected override IAnime WrapItem(IMedia media)
    {
        return new MikibotAnime(media);
    }

    /// <inheritdoc />
    protected override string ExtractId(IAnime item)
    {
        return item.Id;
    }

    private class MikibotAnime
        : IAnime
    {
        public MikibotAnime(IMedia media)
        {
            Id = media.Id.ToString();

            TitleEnglish = media.EnglishTitle;
            TitleJapanese = media.NativeTitle;

            Description = media.Description;
            Url  = media.Url;

            StartDate = null;
            EndDate = null;

            var internalMediaObject = media.GetType().GetField("_media", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(media);
            var internalAdultValue = (bool?)internalMediaObject?.GetType()?.GetField("isAdultContent", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(internalMediaObject) ?? false;
            Adult = internalAdultValue;

            ImageUrl = media.CoverImage;
            Genres = media.Genres;

            if (media.Episodes >= 0)
                TotalEpisodes = (uint)media.Episodes;
            else
                TotalEpisodes = null;
        }

        public string Id { get; }

        public string TitleEnglish { get; }
        public string TitleJapanese { get; }

        public string Description { get; }
        public string Url { get; }

        public DateTimeOffset? StartDate { get; }
        public DateTimeOffset? EndDate { get; }

        public bool Adult { get; }

        public string ImageUrl { get; }

        public IReadOnlyList<string> Genres { get; }
        public uint? TotalEpisodes { get; }
    }
}