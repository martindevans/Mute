using System.Threading.Tasks;

using Miki.Anilist;

namespace Mute.Moe.Services.Information.Anime;

public class MikibotAnilistCharacterSearch
    : BaseMikibotSearchService<ICharacterSearchResult, ICharacter>, ICharacterInfo
{
    protected override async Task<ICharacter?> GetItemAsync(AnilistClient client, ICharacterSearchResult searchItem)
    {
        var item = await client.GetCharacterAsync(searchItem.Id);
        if (item == null)
            return null;

        return new MikibotCharacter(item);
    }

    public Task<ICharacter?> GetCharacterInfoAsync(string search)
    {
        return GetItemInfoAsync(search);
    }

    public IAsyncEnumerable<ICharacter> GetCharactersInfoAsync(string search)
    {
        return GetItemsInfoAsync(search);
    }

    protected override uint Distance(ICharacterSearchResult item, string search)
    {
        var fld = Dist(item.FirstName + " " + item.LastName);
        var lfd = Dist(item.LastName + " " + item.FirstName);

        //Take the min distance to either English style name (Martin Evans) or Japanese style name (Evans Martin)
        return Math.Min(fld, lfd);

        uint Dist(string nm)
        {
            if (nm.Equals(search))
                return 0;
            if (nm.Contains(search))
                return 1;
            return search.Levenshtein(search);
        }
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

    private class MikibotCharacter(Miki.Anilist.ICharacter item)
        : ICharacter
    {
        public string Id { get; } = item.Id.ToString();

        public string GivenName { get; } = item.FirstName;

        public string FamilyName { get; } = item.LastName;

        public string Description { get; } = item.Description;

        public string Url { get; } = item.SiteUrl;

        public string ImageUrl { get; } = item.LargeImageUrl ?? item.MediumImageUrl;
    }
}