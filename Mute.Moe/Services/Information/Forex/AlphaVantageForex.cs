using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using FluidCaching;
using JetBrains.Annotations;
using Mute.Moe.Utilities;
using Newtonsoft.Json;

namespace Mute.Moe.Services.Information.Forex
{
    public class AlphaVantageForex
        : IForexInfo
    {
        private readonly HttpClient _http;
        private readonly AlphaAdvantageConfig _config;

        private readonly FluidCache<IForexQuote> _cache;
        private readonly IIndex<KeyValuePair<string, string>, IForexQuote> _bySymbolPair;

        public AlphaVantageForex([NotNull] Configuration config, IHttpClientFactory http)
        {
            _config = config.AlphaAdvantage;
            _http = http.CreateClient();

            _cache = new FluidCache<IForexQuote>(_config.CacheSize, TimeSpan.FromSeconds(_config.CacheMinAgeSeconds), TimeSpan.FromSeconds(_config.CacheMaxAgeSeconds), () => DateTime.UtcNow);
            _bySymbolPair = _cache.AddIndex("BySymbolPair", a => new KeyValuePair<string, string>(a.FromCode, a.ToCode));
        }

        public async Task<IForexQuote> GetExchangeRate(string fromSymbol, string toSymbol)
        {
            var cached = await _bySymbolPair.GetItem(new KeyValuePair<string, string>(fromSymbol, toSymbol));
            if (cached != null)
                return cached;

            var from = Uri.EscapeUriString(fromSymbol);
            var to = Uri.EscapeUriString(toSymbol);

            using (var result = await _http.GetAsync($"https://www.alphavantage.co/query?function=CURRENCY_EXCHANGE_RATE&from_currency={from}&to_currency={to}&apikey={_config.Key}"))
            {
                if (!result.IsSuccessStatusCode)
                    return null;

                ExchangeRateResponseContainer response;
                var serializer = new JsonSerializer();
                using (var sr = new StreamReader(await result.Content.ReadAsStreamAsync()))
                using (var jsonTextReader = new JsonTextReader(sr))
                    response = serializer.Deserialize<ExchangeRateResponseContainer>(jsonTextReader);

                _cache.Add(response.Response);
                return response.Response;
            }
        }

        #region model
        private class ExchangeRateResponseContainer
        {
            [JsonProperty("Realtime Currency Exchange Rate"), UsedImplicitly]
            public ExchangeRateResponse Response;
        }

        public class ExchangeRateResponse
            : IForexQuote
        {
            [JsonProperty("1. From_Currency Code")] public string FromCode { get; private set; }
            [JsonProperty("2. From_Currency Name")] public string FromName { get; private set; }

            [JsonProperty("3. To_Currency Code")] public string ToCode { get; private set; }
            [JsonProperty("4. To_Currency Name")] public string ToName { get; private set; }

            [JsonProperty("5. Exchange Rate")] public decimal ExchangeRate { get; private set; }

            [JsonProperty("6. Last Refreshed")] public string LastRefreshed { get; private set; }
            [JsonProperty("7. Time Zone")] public string Timezone { get; private set; }
        }
        #endregion
    }
}
