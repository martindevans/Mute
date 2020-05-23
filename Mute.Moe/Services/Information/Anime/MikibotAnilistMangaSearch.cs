using System.Collections.Generic;
using System.Threading.Tasks;

using Miki.Anilist;
using Miki.Anilist.Objects;

namespace Mute.Moe.Services.Information.Anime
{
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

        private class MikibotManga
            : IManga
        {
            public MikibotManga( IMedia media)
            {
                Id = media.Id.ToString();

                TitleEnglish = media.EnglishTitle;
                TitleJapanese = media.NativeTitle;
                Description = media.Description;

                Url  = media.Url;
                ImageUrl = media.CoverImage;

                Chapters = media.Chapters;
                Volumes = media.Volumes;
            }

            public string Id { get; }

            public string TitleEnglish { get; }
            public string TitleJapanese { get; }
            public string Description { get; }

            public string Url { get; }
            public string ImageUrl { get; }

            public int? Chapters { get; }
            public int? Volumes { get; }

            
        }
    }
}
