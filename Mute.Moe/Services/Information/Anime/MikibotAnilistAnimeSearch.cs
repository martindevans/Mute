using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Miki.Anilist;
using Miki.Anilist.Objects;

namespace Mute.Moe.Services.Information.Anime;

public class MikibotAnilistAnimeSearch
    : BaseMikibotMediaSearchService<IAnime>, IAnimeInfo
{
    public MikibotAnilistAnimeSearch()
        : base(MediaFormat.MANGA, MediaFormat.NOVEL, MediaFormat.MUSIC)    //This is a list of formats _not_ to return!
    {
    }

    public Task<IAnime?> GetAnimeInfoAsync(string search)
    {
        return GetItemInfoAsync(search);
    }

    public IAsyncEnumerable<IAnime> GetAnimesInfoAsync(string search)
    {
        return GetItemsInfoAsync(search);
    }

    public IAsyncEnumerable<IAnime> GetAnimesInfoAsync(ICharacter search)
    {
        return GetItemsInfoAsync(search);
    }

    protected override IAnime WrapItem(IMedia media)
    {
        return new MikibotAnime(media);
    }

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

            //Adult = (bool)(media.GetType().GetField("isAdultContent")?.GetValue(media) ?? false);
            Adult = false;

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