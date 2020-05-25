using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using FluidCaching;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Mute.Moe.Services.Information.Stocks
{
    public class AlphaVantageStocks
        : IStockQuotes
    {
        private readonly HttpClient _http;

        private readonly FluidCache<IStockQuote> _cache;
        private readonly IIndex<string, IStockQuote> _bySymbol;
        private readonly string _key;

        public AlphaVantageStocks(Configuration config, IHttpClientFactory http)
        {
            if (config.AlphaAdvantage == null)
                throw new ArgumentNullException(nameof(config.AlphaAdvantage));

            _key = config.AlphaAdvantage.Key ?? throw new ArgumentNullException(nameof(config.AlphaAdvantage));
            _http = http.CreateClient();
            _cache = new FluidCache<IStockQuote>(config.AlphaAdvantage.CacheSize, TimeSpan.FromSeconds(config.AlphaAdvantage.CacheMinAgeSeconds), TimeSpan.FromSeconds(config.AlphaAdvantage.CacheMaxAgeSeconds), () => DateTime.UtcNow);
            _bySymbol = _cache.AddIndex("BySymbol", a => a.Symbol);
        }

        public async Task<IStockQuote?> GetQuote(string stock)
        {
            //https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol=msft&apikey=demo

            var cached = await _bySymbol.GetItem(stock);
            if (cached != null)
                return cached;

            using var result = await _http.GetAsync($"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={Uri.EscapeUriString(stock)}&apikey={_key}");
            if (!result.IsSuccessStatusCode)
                return null;

            StockQuoteResponseContainer? response;
            var serializer = new JsonSerializer();
            using (var sr = new StreamReader(await result.Content.ReadAsStreamAsync()))
            using (var jsonTextReader = new JsonTextReader(sr))
                response = serializer.Deserialize<StockQuoteResponseContainer>(jsonTextReader);

            if (response?.Response?.Symbol == null)
                return null;

            _cache.Add(response.Response);
            return response.Response;
        }

        private class StockQuoteResponseContainer
        {
            [JsonProperty("Global Quote"), UsedImplicitly]
            public StockQuoteResponse? Response;
        }

        public class StockQuoteResponse
            : IStockQuote
        {
            [JsonProperty("01. symbol"), UsedImplicitly] public string Symbol { get; private set; }

            [JsonProperty("02. open"), UsedImplicitly] public decimal Open { get; private set; }
            [JsonProperty("03. high"), UsedImplicitly] public decimal High { get; private set; }
            [JsonProperty("04. low"), UsedImplicitly] public decimal Low { get; private set; }
            [JsonProperty("05. price"), UsedImplicitly] public decimal Price { get; private set; }

            [JsonProperty("06. volume"), UsedImplicitly] public long Volume { get; private set; }

            [JsonProperty("09. change"), UsedImplicitly] public decimal? PctChange24H { get; private set; }
        }
    }
}
