using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluidCaching;
using Miki.Anilist;
using Miki.Anilist.Objects;
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
            return client.SearchMediaAsync(search, index, false, filter: _formats);
        }

        protected override async Task<TItem?> GetItemAsync(AnilistClient client, IMediaSearchResult item)
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

         protected abstract TItem WrapItem( IMedia media);
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

        protected async Task<TItem?> GetItemInfoAsync(string search)
        {
            search = search.ToLowerInvariant();

            var client = new AnilistClient();

            var result = await GetSearchItemsAsync(client, search).FirstOrDefaultAsync();
            if (result == null)
                return null;

            return await GetItemAsyncCached(client, result);
        }

        protected IAsyncEnumerable<TItem> GetItemsInfoAsync(ICharacter character)
        {
            throw new NotImplementedException();
        }

         protected IAsyncEnumerable<TItem> GetItemsInfoAsync(string search)
        {
            var client = new AnilistClient();

            return GetSearchItemsAsync(client, search)
                .OrderBy(i => Distance(i, search))
                .SelectAwait(async i => await GetItemAsyncCached(client, i))
                .Where(a => a != null)
                .Select(a => a!);
        }

        private async IAsyncEnumerable<TSearchItem> GetSearchItemsAsync(AnilistClient client, string search)
        {
            search = search.ToLowerInvariant();

            for (var i = 0; i < 2; i++)
            {
                var page = await SearchPage(client, search, i);

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

        protected abstract uint Distance( TSearchItem item, string search);

        protected abstract string ExtractId( TSearchItem item);

        protected abstract string ExtractId( TItem item);

        protected abstract Task<ISearchResult<TSearchItem>> SearchPage(AnilistClient client, string search, int index);
    }
}
