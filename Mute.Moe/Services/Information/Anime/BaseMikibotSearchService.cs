using System.Text;
using System.Threading.Tasks;
using FluidCaching;
using Miki.Anilist;
using Miki.Anilist.Objects;

namespace Mute.Moe.Services.Information.Anime;

public abstract class BaseMikibotMediaSearchService<TItem>
    : BaseMikibotSearchService<IMediaSearchResult, TItem>
    where TItem : class
{
    private readonly MediaFormat[] _formats;

    protected BaseMikibotMediaSearchService(params MediaFormat[] formats)
    {
        _formats = formats;
    }

    protected override string ExtractId(IMediaSearchResult item)
    {
        return item.Id.ToString();
    }

    protected override Task<ISearchResult<IMediaSearchResult>> SearchPage(AnilistClient client, string search, int index)
    {
        return client.SearchMediaAsync(search, index, false, filter: _formats);
    }

    protected override async Task<TItem?> GetItemAsync(AnilistClient client, IMediaSearchResult item)
    {
        var mediaItem = await client.GetMediaAsync(item.Id);
        if (mediaItem == null)
            return null;

        var sb = new StringBuilder(mediaItem.Description);
        sb.Replace("<b>", "**");
        sb.Replace("</b>", "**");
        sb.Replace("<i>", "_");
        sb.Replace("</i>", "_");
        sb.Replace("\n\n", "\n");

        return WrapItem(new MediaDescriptionReplacement(mediaItem, sb.ToString()));
    }

    protected override uint Distance(IMediaSearchResult item, string searchTerm)
    {
        var itemTitle = item.EnglishTitle?.ToLowerInvariant() ?? "";

        if (itemTitle.Equals(searchTerm))
            return 0;
        if (itemTitle.Contains(searchTerm))
            return 1;
        return itemTitle.Levenshtein(searchTerm);
    }

    protected abstract TItem WrapItem(IMedia media);

    private class MediaDescriptionReplacement
        : IMedia
    {
        private readonly IMedia _media;

        public int Id => _media.Id;

        public MediaType Type => _media.Type;

        public string DefaultTitle => _media.DefaultTitle;

        public string EnglishTitle => _media.EnglishTitle;

        public string NativeTitle => _media.NativeTitle;

        public string RomajiTitle => _media.RomajiTitle;

        public int? Chapters => _media.Chapters;

        public string CoverImage => _media.CoverImage;

        public string Description { get; }

        public int? Duration => _media.Duration;

        public int? Episodes => _media.Episodes;

        public IReadOnlyList<string> Genres => _media.Genres;

        public int? Score => _media.Score;

        public string Status => _media.Status;

        public string Url => _media.Url;

        public int? Volumes => _media.Volumes;

        public MediaDescriptionReplacement(IMedia media, string desc)
        {
            _media = media;
            Description = desc;
        }
    }
}

public abstract class BaseMikibotSearchService<TSearchItem, TItem>
    where TItem : class
    where TSearchItem : class
{
    private readonly FluidCache<TItem> _cache;
    private readonly IIndex<string, TItem> _itemById;

    protected BaseMikibotSearchService()
    {
        _cache = new FluidCache<TItem>(1024, TimeSpan.FromHours(1), TimeSpan.FromDays(7), () => DateTime.UtcNow);
        _itemById = _cache.AddIndex("id", ExtractId);
    }

    protected async Task<TItem?> GetItemInfoAsync(string search)
    {
        search = search.ToLowerInvariant();

        var client = new AnilistClient();

        var result = await GetSearchItemsAsync(client, search).OrderBy(i => Distance(i, search)).Cast<TSearchItem?>().FirstOrDefaultAsync();
        if (result == null)
            return null;

        return await GetItemAsyncCached(client, result);
    }

    protected IAsyncEnumerable<TItem> GetItemsInfoAsync(string search)
    {
        var client = new AnilistClient();

        return GetSearchItemsAsync(client, search)
            .SelectAwait(async i => await GetItemAsyncCached(client, i))
            .Where(a => a != null)
            .Select(a => a!);
    }

    private async IAsyncEnumerable<TSearchItem> GetSearchItemsAsync(AnilistClient client, string search)
    {
        search = search.ToLowerInvariant();

        var index = 0;
        while (true)
        {
            var page = await SearchPage(client, search, index++);

            foreach (var item in page.Items)
                yield return item;

            if (!page.PageInfo.HasNextPage)
                break;
        }
    }

    private async Task<TItem?> GetItemAsyncCached(AnilistClient client, TSearchItem searchItem)
    {
        var cached = await _itemById.GetItem(ExtractId(searchItem));
        if (cached != null)
            return cached;

        var item = await GetItemAsync(client, searchItem);
        if (item == null)
            return null;

        _cache.Add(item);

        return item;
    }

    protected abstract Task<TItem?> GetItemAsync(AnilistClient client, TSearchItem searchItem);

    protected abstract uint Distance(TSearchItem item, string search);

    protected abstract string ExtractId(TSearchItem item);

    protected abstract string ExtractId(TItem item);

    protected abstract Task<ISearchResult<TSearchItem>> SearchPage(AnilistClient client, string search, int index);
}