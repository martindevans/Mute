using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluidCaching;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Mute.Moe.Services.Information.Cryptocurrency
{
    /// <summary>
    /// https://coinmarketcap.com/api/documentation/v1/#section/Quick-Start-Guide
    /// </summary>
    public class ProCoinMarketCapCrypto
        : ICryptocurrencyInfo
    {
        private readonly FluidCache<ICurrency> _currencyCache;
        private readonly IIndex<uint, ICurrency> _currencyById;
        private readonly IIndex<string, ICurrency> _currencyByName;
        private readonly IIndex<string, ICurrency> _currencyBySymbol;

        private readonly FluidCache<ITicker> _tickerCache;
        private readonly IIndex<string, ITicker> _tickerBySymbol;

        private readonly ConcurrentDictionary<string, string> _nameToSymbolMap = new ConcurrentDictionary<string, string>();

        private readonly HttpClient _http;
        private readonly string _key;

        public ProCoinMarketCapCrypto( Configuration config,  IHttpClientFactory http)
        {
            if (config.CoinMarketCap == null)
                throw new ArgumentNullException(nameof(config.CoinMarketCap));

            _key = config.CoinMarketCap?.Key ?? throw new ArgumentNullException(nameof(config.CoinMarketCap.Key));
            _http = http.CreateClient();
            
            _currencyCache = new FluidCache<ICurrency>(config.CoinMarketCap.CacheSize, TimeSpan.FromSeconds(config.CoinMarketCap.CacheMinAgeSeconds), TimeSpan.FromSeconds(config.CoinMarketCap.CacheMaxAgeSeconds), () => DateTime.UtcNow);
            _currencyById = _currencyCache.AddIndex("IndexByUniqueId", a => a.Id);
            _currencyByName = _currencyCache.AddIndex("IndexByName", a => a.Name);
            _currencyBySymbol = _currencyCache.AddIndex("IndexBySymbol", a => a.Symbol);

            _tickerCache = new FluidCache<ITicker>(config.CoinMarketCap.CacheSize, TimeSpan.FromSeconds(config.CoinMarketCap.CacheMinAgeSeconds), TimeSpan.FromSeconds(config.CoinMarketCap.CacheMaxAgeSeconds), () => DateTime.UtcNow);
            _tickerBySymbol = _tickerCache.AddIndex("IndexBySymbol", a => a.Currency.Symbol);
        }

        public async Task<ICurrency?> FindBySymbol(string symbol)
        {
            symbol = Uri.EscapeUriString(symbol.ToUpperInvariant());
            var url = $"https://pro-api.coinmarketcap.com/v1/cryptocurrency/info?CMC_PRO_API_KEY={_key}&symbol={symbol}";
            return await GetOrDownload<string, ICurrency, CmcCurrencyResponse>(symbol, _currencyCache, _currencyBySymbol, _ => _http.GetAsync(url), a => a.Data.Values);
        }

        public async Task<ICurrency?> FindById(uint id)
        {
            var url = $"https://pro-api.coinmarketcap.com/v1/cryptocurrency/info?CMC_PRO_API_KEY={_key}&id={id}";
            return await GetOrDownload<uint, ICurrency, CmcCurrencyResponse>(id, _currencyCache, _currencyById, _ => _http.GetAsync(url), a => a.Data.Values);
        }

        private static async Task<TItem?> GetOrDownload<TKey, TItem, TResponse>(TKey key, FluidCache<TItem> cache, IIndex<TKey, TItem> index, Func<TKey, Task<HttpResponseMessage>> download, Func<TResponse, IEnumerable<TItem>> extract)
            where TItem : class
            where TResponse : class
        {
            //Get it from the cache if possible
            var item = await index.GetItem(key);
            if (item != null)
                return item;

            //No luck, download the details from the API
            var result = await download(key);
            if (!result.IsSuccessStatusCode)
                return null;

            //Deserialize response
            TResponse? model;
            var serializer = new JsonSerializer();
            using (var sr = new StreamReader(await result.Content.ReadAsStreamAsync()))
            using (var jsonTextReader = new JsonTextReader(sr))
                model = serializer.Deserialize<TResponse>(jsonTextReader);

            if (model == null)
                return null;

            var extracted = extract(model);
            if (extracted == null)
                return null;

            //That could get several currencies, add them all to cache
            foreach (var extractedItem in extracted)
                cache.Add(extractedItem);

            //Get the item from cache again, hopefully with more success this time
            return await index.GetItem(key);
        }

        public async Task<ICurrency?> FindByName(string name)
        {
            // Try to get cached data
            name = name.ToLowerInvariant();
            if (_nameToSymbolMap.TryGetValue(name, out var symbol))
                return await FindBySymbol(symbol);

            //No luck, download the details from the API
            var url = $"https://pro-api.coinmarketcap.com/v1/cryptocurrency/map?CMC_PRO_API_KEY={_key}";
            using var result = await _http.GetAsync(url);
            if (!result.IsSuccessStatusCode)
                return null;

            //Deserialize response
            CmcMapResponse? model;
            var serializer = new JsonSerializer();
            using (var sr = new StreamReader(await result.Content.ReadAsStreamAsync()))
            using (var jsonTextReader = new JsonTextReader(sr))
                model = serializer.Deserialize<CmcMapResponse>(jsonTextReader);

            if (model == null)
                return null;

            //Store the entire map
            foreach (var cmcMap in model.Data)
                _nameToSymbolMap[cmcMap.Name.ToLowerInvariant()] = cmcMap.Symbol;

            //Get it from the cache again
            if (_nameToSymbolMap.TryGetValue(name, out symbol))
                return await FindBySymbol(symbol);
            else
                return null;
        }

        public async Task<ICurrency?> FindBySymbolOrName(string symbolOrName)
        {
            //Consult caches first
            var cacheSym = await _currencyBySymbol.GetItem(symbolOrName);
            if (cacheSym != null)
                return cacheSym;

            var cacheName = await _currencyByName.GetItem(symbolOrName);
            if (cacheName != null)
                return cacheName;

            //Find by symbol first (narrower search, more likely to succeed)
            var bySymbol = await FindBySymbol(symbolOrName);
            if (bySymbol != null)
                return bySymbol;

            //Ok we'll have to try by name then
            return await FindByName(symbolOrName);
        }

        public async Task<ITicker?> GetTicker(ICurrency currency, string? quote = null)
        {
            async Task<HttpResponseMessage> Download(string sym)
            {
                //An API access costs 1 access token on the billing per 100 items queried, rounded up. We're only asking for 1 item here which is a huge waste.
                //Add on 99 random symbols so we can cache them and maybe save a token in the future.
                var random = new Random();
                var tokens = new List<string> { sym };
                var countRemaining = _nameToSymbolMap.Count;
                foreach (var symbol in _nameToSymbolMap.Values)
                {
                    //If we run out of items break out immediately
                    if (countRemaining == 0)
                        break;

                    //Also break if we have enough items already
                    if (tokens.Count == 100)
                        break;

                    //This isn't an even distribution, but who cares?
                    if (random.NextDouble() < ((100f - tokens.Count) / countRemaining))
                        tokens.Add(symbol);
                    countRemaining--;
                }

                var allTokens = string.Join(",", tokens.Distinct().Select(Uri.EscapeUriString));
                var uri = $"https://pro-api.coinmarketcap.com/v1/cryptocurrency/quotes/latest?CMC_PRO_API_KEY={_key}&symbol={allTokens}";

                return await _http.GetAsync(uri);
            }

            return await GetOrDownload<string, ITicker, CmcTickerResponse>(currency.Symbol, _tickerCache, _tickerBySymbol, Download, r => r.Data.Values);
        }

        #region model
        private class Status
        {
            [JsonProperty("error_message"), UsedImplicitly] public string? Error { get; private set; }
        }

        private class CmcCurrency
            : ICurrency
        {
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable 0649 // Field not assigned
            [JsonProperty("id")] private uint? _id;
            [JsonProperty("symbol")] private string? _symbol;
            [JsonProperty("name")] private string? _name;
#pragma warning restore 0649 // Field not assigned
#pragma warning restore IDE0044 // Add readonly modifier

            public uint Id => _id ?? throw new InvalidOperationException("API returned null value for `id` field");
            public string Symbol => _symbol ?? throw new InvalidOperationException("API returned null value for `symbol` field");
            public string Name => _name ?? throw new InvalidOperationException("API returned null value for `name` field");
        }

        private class CmcCurrencyResponse
        {
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable 0649 // Field not assigned
            [JsonProperty("status")] private Status? _status;
            [JsonProperty("data")] private Dictionary<string, CmcCurrency>? _data;
#pragma warning restore 0649 // Field not assigned
#pragma warning restore IDE0044 // Add readonly modifier

            public Status Status => _status ?? throw new InvalidOperationException("API returned null value for `status` field");
            public Dictionary<string, CmcCurrency> Data => _data ?? throw new InvalidOperationException("API returned null value for `data` field");
        }

        private class CmcMap
        {
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable 0649 // Field not assigned
            [JsonProperty("id")] private uint? _id;
            [JsonProperty("symbol")] private string? _symbol;
            [JsonProperty("name")] private string? _name;
#pragma warning restore 0649 // Field not assigned
#pragma warning restore IDE0044 // Add readonly modifier

            public uint Id => _id ?? throw new InvalidOperationException("API returned null value for `id` field");
            public string Symbol => _symbol ?? throw new InvalidOperationException("API returned null value for `symbol` field");
            public string Name => _name ?? throw new InvalidOperationException("API returned null value for `name` field");
        }

        private class CmcMapResponse
        {
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable 0649 // Field not assigned
            [JsonProperty("status"), UsedImplicitly] private Status? _status;
            [JsonProperty("data"), UsedImplicitly] private CmcMap[]? _data;
#pragma warning restore 0649 // Field not assigned
#pragma warning restore IDE0044 // Add readonly modifier

            public Status Status => _status ?? throw new InvalidOperationException("API returned null value for `status` field");
            public CmcMap[] Data => _data ?? throw new InvalidOperationException("API returned null value for `data` field");
        }

        private class CmcQuote
            : IQuote
        {
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable 0649 // Field not assigned
            [JsonProperty("price"), UsedImplicitly] private decimal? _price;
#pragma warning restore 0649 // Field not assigned
#pragma warning restore IDE0044 // Add readonly modifier

            public decimal Price => _price ?? throw new InvalidOperationException("API returned null value for `price` field");

            [JsonProperty("volume_24h"), UsedImplicitly] public decimal? Volume24H { get; private set; }
            [JsonProperty("percent_change_1h"), UsedImplicitly] public decimal? PctChange1H { get; private set; }
            [JsonProperty("percent_change_24h"), UsedImplicitly] public decimal? PctChange24H { get; private set; }
            [JsonProperty("percent_change_7d"), UsedImplicitly] public decimal? PctChange7D { get; private set; }
        }

        private class CmcTicker
            : ITicker, ICurrency
        {
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable 0649 // Field not assigned
            [JsonProperty("id"), UsedImplicitly] private uint? _id;
            [JsonProperty("name"), UsedImplicitly] private string? _name;
            [JsonProperty("symbol"), UsedImplicitly] private string? _symbol;
            [JsonProperty("quote"), UsedImplicitly] private Dictionary<string, CmcQuote>? _cmcQuotes;
#pragma warning restore 0649 // Field not assigned
#pragma warning restore IDE0044 // Add readonly modifier

            public uint Id => _id ?? throw new InvalidOperationException("API returned null value for `id` field");
            public string Name => _name ?? throw new InvalidOperationException("API returned null value for `name` field");
            public string Symbol => _symbol ?? throw new InvalidOperationException("API returned null value for `symbol` field");

            [JsonProperty("circulating_supply"), UsedImplicitly] public decimal? CirculatingSupply { get; private set; }
            [JsonProperty("total_supply"), UsedImplicitly] public decimal? TotalSupply { get; private set; }
            [JsonProperty("max_supply"), UsedImplicitly] public decimal? MaxSupply { get; private set; }

            private IReadOnlyDictionary<string, IQuote>? _quotesCache;
            public IReadOnlyDictionary<string, IQuote> Quotes
            {
                get
                {
                    if (_quotesCache == null)
                    {
                        var cmcQuotes = _cmcQuotes ?? throw new InvalidOperationException("API returned null value for `quote` field");
                        _quotesCache = new Dictionary<string, IQuote>(cmcQuotes.Select(a => new KeyValuePair<string, IQuote>(a.Key, a.Value)));
                    }

                    return _quotesCache;
                }
            }

            ICurrency ITicker.Currency => this;
        }

        private class CmcTickerResponse
        {
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable 0649 // Field not assigned
            [JsonProperty("status"), UsedImplicitly] private Status? _status;
            [JsonProperty("data"), UsedImplicitly] private Dictionary<string, CmcTicker>? _data;
#pragma warning restore 0649 // Field not assigned
#pragma warning restore IDE0044 // Add readonly modifier

            public Status Status => _status ?? throw new InvalidOperationException("API returned null value for `status` field");
            public Dictionary<string, CmcTicker> Data => _data ?? throw new InvalidOperationException("API returned null value for `data` field");
        }
        #endregion
    }
}
