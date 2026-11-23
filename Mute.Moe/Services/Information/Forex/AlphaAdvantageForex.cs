using FluidCaching;
using Humanizer;
using Newtonsoft.Json;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Information.Forex;

/// <inheritdoc />
public class AlphaAdvantageForex
    : IForexInfo
{
    private readonly HttpClient _http;
    private readonly string _key;

    private readonly FluidCache<IForexQuote> _cache;
    private readonly IIndex<KeyValuePair<string, string>, IForexQuote> _bySymbolPair;

    private readonly Task<HashSet<string>> _validCodes;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="config"></param>
    /// <param name="http"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public AlphaAdvantageForex(Configuration config, IHttpClientFactory http)
    {
        _key = config.AlphaAdvantage?.Key ?? throw new ArgumentNullException(nameof(config.AlphaAdvantage.Key));
        _http = http.CreateClient();

        _cache = new FluidCache<IForexQuote>(config.AlphaAdvantage.CacheSize, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(config.AlphaAdvantage.CacheMaxAgeSeconds), () => DateTime.UtcNow);
        _bySymbolPair = _cache.AddIndex("BySymbolPair", a => new KeyValuePair<string, string>(a.FromCode, a.ToCode));

        _validCodes = GetCurrencyCodeSet();
    }

    /// <inheritdoc />
    public async Task<IForexQuote?> GetExchangeRate(string fromSymbol, string toSymbol)
    {
        // Check that the inputs are valid
        var valid = await _validCodes;
        if (!valid.Contains(fromSymbol) || !valid.Contains(toSymbol))
            return null;

        // Try to get it from the cache
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

    private async Task<HashSet<string>> GetCurrencyCodeSet()
    {
        var physical = await GetCurrencyList("https://www.alphavantage.co/physical_currency_list/");
        var crypto = await GetCurrencyList("https://www.alphavantage.co/cryptocurrency_list/");

        return
        [
            ..physical.Concat(crypto).Select(a => a.Code),
        ];
    }

    private async Task<IReadOnlyList<Currency>> GetCurrencyList(string url)
    {
        using var result = await _http.GetAsync(url);
        if (!result.IsSuccessStatusCode)
            return [];

        using var reader = new StreamReader(await result.Content.ReadAsStreamAsync());

        // Read header ("currency code,currency name")
        await reader.ReadLineAsync();

        // Read and parse lines
        var results = new List<Currency>();
        while (true)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line))
                break;

            var splits = line.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (splits.Length != 2)
                continue;

            var code = splits[0];
            var name = splits[1];
            results.Add(new Currency(code, name));
        }

        return results;
    }

    private record Currency(string Code, string Name);

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

    private class ExchangeRateResponse
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