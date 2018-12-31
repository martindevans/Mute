using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Mute.Services
{
    public class CryptoCurrencyService
    {
        #region data model
        public class Currency
        {
            [UsedImplicitly, JsonProperty("id")] public int Id { get; private set; }
            [UsedImplicitly, JsonProperty("name")] public string Name { get; private set; }
            [UsedImplicitly, JsonProperty("symbol")] public string Symbol { get; private set; }
        }

        private class Listings
        {
            [UsedImplicitly, JsonProperty("data")] public Currency[] Data;
        }

        public class Quote
        {
            [UsedImplicitly, JsonProperty("price")] public decimal Price { get; private set; }
            [UsedImplicitly, JsonProperty("volume_24h")] public decimal? Volume { get; private set; }
            [UsedImplicitly, JsonProperty("market_cap")] public decimal MarketCap { get; private set; }
            [UsedImplicitly, JsonProperty("percent_change_1h")] public decimal? PctChange1H { get; private set; }
            [UsedImplicitly, JsonProperty("percent_change_24h")] public decimal? PctChange24H { get; private set; }
            [UsedImplicitly, JsonProperty("percent_change_7d")] public decimal? PctChange7D { get; private set; }
        }

        public class Ticker
        {
            [UsedImplicitly, JsonProperty("id")] public int Id { get; private set; }
            [UsedImplicitly, JsonProperty("name")] public string Name { get; private set; }
            [UsedImplicitly, JsonProperty("symbol")] public string Symbol { get; private set; }
            [UsedImplicitly, JsonProperty("rank")] public int Rank { get; private set; }

            [UsedImplicitly, JsonProperty("circulating_supply")] public decimal CirculatingSupply { get; private set; }
            [UsedImplicitly, JsonProperty("total_supply")] public decimal TotalSupply { get; private set; }
            [UsedImplicitly, JsonProperty("max_supply")] public decimal? MaxSupply { get; private set; }

            [UsedImplicitly, JsonProperty("quotes")] private Dictionary<string, Quote> _quotes;
            public IReadOnlyDictionary<string, Quote> Quotes => _quotes;
        }

        private class TickerContainer
        {
            [UsedImplicitly, JsonProperty("data")] public Ticker Data;
        }
        #endregion

        private class ListingsData
        {
            public DateTime DataFetchedAtUtc { get; }

            public IReadOnlyDictionary<int, Currency> IdToListing { get; }
            public IReadOnlyDictionary<string, Currency> NameToListing { get; }
            public IReadOnlyDictionary<string, Currency> SymbolToListing { get; }

            public ListingsData([NotNull] Currency[] data)
            {
                DataFetchedAtUtc = DateTime.UtcNow;

                IdToListing = data.ToDictionary(a => a.Id, a => a);
                NameToListing = data.GroupBy(a => a.Name.ToLowerInvariant()).ToDictionary(a => a.Key, a => a.First());
                SymbolToListing = data.GroupBy(a => a.Symbol.ToLowerInvariant()).ToDictionary(a => a.Key, a => a.First());
            }
        }

        private ListingsData _listings;
        private readonly IHttpClient _http;

        public CryptoCurrencyService(IHttpClient http)
        {
            _http = http;
        }

        private async Task RefreshListing()
        {
            var prev = _listings;
            if (prev == null || (DateTime.UtcNow - prev.DataFetchedAtUtc > TimeSpan.FromDays(1)))
            {
                using (var getResult = await _http.GetAsync("https://api.coinmarketcap.com/v2/listings/"))
                {
                    var jsonResult = JsonConvert.DeserializeObject<Listings>(await getResult.Content.ReadAsStringAsync());

                    Interlocked.CompareExchange(ref _listings, new ListingsData(jsonResult.Data), prev);
                }
            }
        }

        public async Task<Currency> FindBySymbol([NotNull] string symbol)
        {
            await RefreshListing();

            if (_listings.SymbolToListing.TryGetValue(symbol.ToLowerInvariant(), out var listing))
                return listing;
            return null;
        }

        public async Task<Currency> FindByName([NotNull] string name)
        {
            await RefreshListing();

            if (_listings.NameToListing.TryGetValue(name.ToLowerInvariant(), out var listing))
                return listing;
            return null;
        }

        public async Task<Currency> FindById(int id)
        {
            await RefreshListing();

            if (_listings.IdToListing.TryGetValue(id, out var listing))
                return listing;
            return null;
        }

        public async Task<Ticker> GetTicker([NotNull] Currency currency, [CanBeNull] string quote = null)
        {
            using (var getResult = await _http.GetAsync($"https://api.coinmarketcap.com/v2/ticker/{currency.Id}/?convert={quote ?? "btc"}"))
            {
                var jsonString = await getResult.Content.ReadAsStringAsync();
                var jsonResult = JsonConvert.DeserializeObject<TickerContainer>(jsonString);
                return jsonResult.Data;
            }
        }

        public async Task<Currency> Find([NotNull] string symbolOrName)
        {
            return await FindByName(symbolOrName)
                ?? await FindBySymbol(symbolOrName);
        }
    }
}
