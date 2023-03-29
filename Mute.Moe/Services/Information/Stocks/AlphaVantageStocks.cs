using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using FluidCaching;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Mute.Moe.Services.Information.Stocks;

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

        using var result = await _http.GetAsync($"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={Uri.EscapeDataString(stock)}&apikey={_key}");
        if (!result.IsSuccessStatusCode)
            return null;

        StockQuoteResponseContainer? response;
        var serializer = new JsonSerializer();
        using (var sr = new StreamReader(await result.Content.ReadAsStreamAsync()))
        await using (var jsonTextReader = new JsonTextReader(sr))
            response = serializer.Deserialize<StockQuoteResponseContainer>(jsonTextReader);

        if (response?.Response?.Symbol == null)
            return null;

        _cache.Add(response.Response);
        return response.Response;
    }

    private class StockQuoteResponseContainer
    {
#pragma warning disable 0649 // Field not assigned
        [JsonProperty("Global Quote"), UsedImplicitly]
        public StockQuoteResponse? Response;
#pragma warning restore 0649 // Field not assigned
    }

    public class StockQuoteResponse
        : IStockQuote
    {
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable 0649 // Field not assigned
        [JsonProperty("01. symbol"), UsedImplicitly] private string? _symbol;
        [JsonProperty("02. open"), UsedImplicitly] private decimal? _open;
        [JsonProperty("03. high"), UsedImplicitly] private decimal? _high;
        [JsonProperty("04. low"), UsedImplicitly] private decimal? _low;
        [JsonProperty("05. price"), UsedImplicitly] private decimal? _price;
        [JsonProperty("06. volume"), UsedImplicitly] private long? _volume;
#pragma warning restore 0649 // Field not assigned
#pragma warning restore IDE0044 // Add readonly modifier

        public string Symbol => _symbol ?? throw new InvalidOperationException("API returned null value for `01. symbol` field");
        public decimal Open => _open ?? throw new InvalidOperationException("API returned null value for `02. open` field");
        public decimal High => _high ?? throw new InvalidOperationException("API returned null value for `03. high` field");
        public decimal Low => _low ?? throw new InvalidOperationException("API returned null value for `04. low` field");
        public decimal Price => _price ?? throw new InvalidOperationException("API returned null value for `05. price` field");
        public long Volume => _volume ?? throw new InvalidOperationException("API returned null value for `06. volume` field");

        [JsonProperty("09. change"), UsedImplicitly] public decimal? PctChange24H { get; private set; }
    }
}