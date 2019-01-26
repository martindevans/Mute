using System.Threading.Tasks;
using JetBrains.Annotations;
using Miki.Anilist;

namespace Mute.Moe.Services.Information.Anime
{
    public class MikibotAnilistMangaSearch
        : BaseMikibotMediaSearchService<IManga>, IMangaInfo
    {
        public MikibotAnilistMangaSearch(IHttpClient http)
            : base(MediaFormat.MANGA, MediaFormat.NOVEL)
        {
        }

        [NotNull] public Task<IManga> GetMangaInfoAsync(string search)
        {
            return GetItemInfoAsync(search);
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
            public MikibotManga([NotNull] IMedia media)
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
