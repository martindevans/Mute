using System;
using System.Linq;
using System.Threading.Tasks;
using FluidCaching;
using JetBrains.Annotations;
using Miki.Anilist;
using Mute.Moe.AsyncEnumerable;
using Mute.Moe.Extensions;

namespace Mute.Moe.Services.Information.Anime
{
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
            return client.SearchMediaAsync(search, index, false, _formats);
        }

        protected override async Task<TItem> GetItemAsync(AnilistClient client, IMediaSearchResult item)
        {
            var mediaItem = await client.GetMediaAsync(item.Id);
            if (mediaItem == null)
                return null;

            return WrapItem(mediaItem);
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

        [NotNull] protected abstract TItem WrapItem([NotNull] IMedia media);
    }

    public abstract class BaseMikibotSearchService<TSearchItem, TItem>
        where TItem : class
        where TSearchItem : class
    {
        private readonly FluidCache<TItem> _cache;
        private readonly IIndex<string, TItem> _itemById;

        protected BaseMikibotSearchService()
        {
            _cache = new FluidCache<TItem>(128, TimeSpan.FromHours(1), TimeSpan.FromDays(7), () => DateTime.UtcNow);
            _itemById = _cache.AddIndex("id", ExtractId);
        }

        [ItemCanBeNull] protected async Task<TItem> GetItemInfoAsync(string search)
        {
            search = search.ToLowerInvariant();

            var client = new AnilistClient();

            async Task<ISearchResult<TSearchItem>> Page(ISearchResult<TSearchItem> previous)
            {
                //Stop once there is no next page
                if (!(previous?.PageInfo.HasNextPage ?? true))
                    return null;

                //Get next page
                var index = (previous?.PageInfo.CurrentPage ?? -1) + 1;
                var r = await SearchPage(client, search, index);
                return r;
            }

            //Search for results, 
            var results = await new SearchResultAsyncEnumerable<TSearchItem>(Page, 2)
                .Select(r => new {r, l = Distance(r, search) })
                .ToArray();

            if (results.Length == 0)
                return null;

            //return the one with the closest levenshtein distance
            var bestResult = results.Aggregate((a, b) => a.l < b.l ? a : b);

            return await GetItemAsyncCached(client, bestResult.r);
        }

        [ItemCanBeNull] private async Task<TItem> GetItemAsyncCached([NotNull] AnilistClient client, [NotNull] TSearchItem searchItem)
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

        [ItemCanBeNull] protected abstract Task<TItem> GetItemAsync([NotNull] AnilistClient client, [NotNull] TSearchItem searchItem);

        protected abstract uint Distance([NotNull] TSearchItem item, string search);

        protected abstract string ExtractId([NotNull] TSearchItem item);

        protected abstract string ExtractId([NotNull] TItem item);

        protected abstract Task<ISearchResult<TSearchItem>> SearchPage([NotNull] AnilistClient client, string search, int index);

        private class SearchResultAsyncEnumerable<T>
            : PagedAsyncEnumerable<ISearchResult<T>, T>
        {
            public SearchResultAsyncEnumerable(Func<ISearchResult<T>, Task<ISearchResult<T>>> page, uint batchLimit = uint.MaxValue)
                : base(page, p => p.Items.GetEnumerator(), batchLimit)
            {
            }
        }
    }
}
