using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Miki.Anilist;
using Mute.Moe.Extensions;

namespace Mute.Moe.Services.Information.Anime
{
    public class MikibotAnilistCharacterSearch
        : BaseMikibotSearchService<ICharacterSearchResult, ICharacter>, ICharacterInfo
    {
        protected override async Task<ICharacter> GetItemAsync(AnilistClient client, ICharacterSearchResult searchItem)
        {
            var item = await client.GetCharacterAsync(searchItem.Id);
            if (item == null)
                return null;

            return new MikibotCharacter(item);
        }

        [NotNull] public Task<ICharacter> GetCharacterInfoAsync(string search)
        {
            return GetItemInfoAsync(search);
        }

        protected override uint Distance(ICharacterSearchResult item, string search)
        {
            uint D(string nm)
            {
                if (nm.Equals(search))
                    return 0;
                if (nm.Contains(search))
                    return 1;
                return search.Levenshtein(search);
            }

            var fld = D(item.FirstName + " " + item.LastName);
            var lfd = D(item.LastName + " " + item.FirstName);

            //Take the min distance to either English style name (Martin Evans) or Japanese style name (Evans Martin)
            return Math.Min(fld, lfd);
        }

        protected override string ExtractId(ICharacterSearchResult item)
        {
            return item.Id.ToString();
        }

        protected override string ExtractId(ICharacter item)
        {
            return item.Id;
        }

        protected override Task<ISearchResult<ICharacterSearchResult>> SearchPage(AnilistClient client, string search, int index)
        {
            return client.SearchCharactersAsync(search, index);
        }

        private class MikibotCharacter
            : ICharacter
        {
            public MikibotCharacter([NotNull] Miki.Anilist.ICharacter item)
            {
                Id = item.Id.ToString();

                GivenName = item.FirstName;
                FamilyName = item.LastName;

                Url = item.SiteUrl;
                ImageUrl = item.LargeImageUrl ?? item.MediumImageUrl;

                Description = item.Description;
            }

            public string Id { get; }

            public string GivenName { get; }
            public string FamilyName { get; }

            public string Description { get; }

            public string Url { get; }
            public string ImageUrl { get; }
        }

        
    }
}
