using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using FluidCaching;
using Newtonsoft.Json;

namespace Mute.Moe.Services.Information.Forex;

public class AlphaVantageForex
    : IForexInfo
{
    private readonly HttpClient _http;
    private readonly string _key;

    private readonly FluidCache<IForexQuote> _cache;
    private readonly IIndex<KeyValuePair<string, string>, IForexQuote> _bySymbolPair;
        
    public AlphaVantageForex(Configuration config, IHttpClientFactory http)
    {
        _key = config.AlphaAdvantage?.Key ?? throw new ArgumentNullException(nameof(config.AlphaAdvantage.Key));
        _http = http.CreateClient();

        _cache = new FluidCache<IForexQuote>(config.AlphaAdvantage.CacheSize, TimeSpan.FromSeconds(config.AlphaAdvantage.CacheMinAgeSeconds), TimeSpan.FromSeconds(config.AlphaAdvantage.CacheMaxAgeSeconds), () => DateTime.UtcNow);
        _bySymbolPair = _cache.AddIndex("BySymbolPair", a => new KeyValuePair<string, string>(a.FromCode, a.ToCode));
    }

    public async Task<IForexQuote?> GetExchangeRate(string fromSymbol, string toSymbol)
    {
        var cached = await _bySymbolPair.GetItem(new KeyValuePair<string, string>(fromSymbol, toSymbol));
        if (cached != null)
            return cached;

        var from = Uri.EscapeDataString(fromSymbol);
        var to = Uri.EscapeDataString(toSymbol);

        using var result = await _http.GetAsync($"https://www.alphavantage.co/query?function=CURRENCY_EXCHANGE_RATE&from_currency={from}&to_currency={to}&apikey={_key}");
        if (!result.IsSuccessStatusCode)
            return null;

        ExchangeRateResponseContainer? response;
        var serializer = new JsonSerializer();
        using (var sr = new StreamReader(await result.Content.ReadAsStreamAsync()))
        await using (var jsonTextReader = new JsonTextReader(sr))
            response = serializer.Deserialize<ExchangeRateResponseContainer?>(jsonTextReader);

        if (response is not { ErrorMessage: null })
            return null;

        _cache.Add(response.Response);
        return response.Response;
    }

    #region model
    private class ExchangeRateResponseContainer
    {
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable 0649 // Field not assigned
        [JsonProperty("Realtime Currency Exchange Rate"), UsedImplicitly] private ExchangeRateResponse? _response;
        [JsonProperty("Error Message"), UsedImplicitly] private string? _error;
#pragma warning restore 0649 // Field not assigned
#pragma warning restore IDE0044 // Add readonly modifier

        public ExchangeRateResponse Response => _response ?? throw new InvalidOperationException("API returned null value for `Realtime Currency Exchange Rate` field");

        // ReSharper disable once ConvertToAutoProperty
        public string? ErrorMessage => _error;
    }

    public class ExchangeRateResponse
        : IForexQuote
    {
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable 0649 // Field not assigned
        [JsonProperty("1. From_Currency Code")] private string? _fromCode;
        [JsonProperty("2. From_Currency Name")] private string? _fromName;
        [JsonProperty("3. To_Currency Code")] private string? _toCode;
        [JsonProperty("4. To_Currency Name")] private string? _toName;
        [JsonProperty("5. Exchange Rate")] private decimal? _exchangeRate;
        [JsonProperty("6. Last Refreshed")] private string? _lastRefreshed;
        [JsonProperty("7. Time Zone")] private string? _timezone;
#pragma warning restore 0649 // Field not assigned
#pragma warning restore IDE0044 // Add readonly modifier

        public string FromCode => _fromCode ?? throw new InvalidOperationException("API returned null value for `1. From_Currency Code` field");
        public string FromName => _fromName ?? throw new InvalidOperationException("API returned null value for `2. From_Currency Name` field");

        public string ToCode => _toCode ?? throw new InvalidOperationException("API returned null value for `3. To_Currency Code` field");
        public string ToName => _toName ?? throw new InvalidOperationException("API returned null value for `4. To_Currency Name` field");

        public decimal ExchangeRate => _exchangeRate ?? throw new InvalidOperationException("API returned null value for `5. Exchange Rate` field");

        public string LastRefreshed => _lastRefreshed ?? throw new InvalidOperationException("API returned null value for `6. Last Refreshed` field");
        public string Timezone => _timezone ?? throw new InvalidOperationException("API returned null value for `7. Time Zone` field");
    }
    #endregion
}