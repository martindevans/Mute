using System;
using System.Net.Http;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Mute.Services
{
    //https://www.alphavantage.co/documentation/#fx

    public class AlphaAdvantageService
    {
        private readonly AlphaAdvantageConfig _config;
        private readonly IHttpClient _http;

        public AlphaAdvantageService([NotNull] Configuration config, [NotNull] IHttpClient http)
        {
            _config = config.AlphaAdvantage;
            _http = http;
        }

        [ItemCanBeNull] public async Task<ExchangeRateResponse> CurrencyExchangeRate(string currencyFrom, string currencyTo)
        {
            //https://www.alphavantage.co/query?function=CURRENCY_EXCHANGE_RATE&from_currency=BTC&to_currency=CNY&apikey=demo

            var from = Uri.EscapeUriString(currencyFrom);
            var to = Uri.EscapeUriString(currencyTo);

            using (var getResult = await _http.GetAsync($"https://www.alphavantage.co/query?function=CURRENCY_EXCHANGE_RATE&from_currency={from}&to_currency={to}&apikey={_config.Key}"))
            {
                try
                {
                    var jsonString = await getResult.Content.ReadAsStringAsync();
                    var jsonResult = JsonConvert.DeserializeObject<ExchangeRateResponseContainer>(jsonString);
                    return jsonResult.Response;
                }
                catch (JsonException)
                {
                    return null;
                }
            }
        }

        private class ExchangeRateResponseContainer
        {
            [JsonProperty("Realtime Currency Exchange Rate")]
            public ExchangeRateResponse Response;
        }

        public class ExchangeRateResponse
        {
            [JsonProperty("1. From_Currency Code")] public string FromCode { get; private set; }
            [JsonProperty("2. From_Currency Name")] public string FromName { get; private set; }

            [JsonProperty("3. To_Currency Code")] public string ToCode { get; private set; }
            [JsonProperty("4. To_Currency Name")] public string ToName { get; private set; }

            [JsonProperty("5. Exchange Rate")] public decimal ExchangeRate { get; private set; }

            [JsonProperty("6. Last Refreshed")] public string LastRefreshed { get; private set; }
            [JsonProperty("7. Time Zone")] public string Timezone { get; private set; }
        }

        [ItemCanBeNull] public async Task<StockQuoteResponse> StockQuote(string stock)
        {
            //https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol=msft&apikey=demo

            using (var getResult = await _http.GetAsync($"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={Uri.EscapeUriString(stock)}&apikey={_config.Key}"))
            {
                try
                {
                    var jsonString = await getResult.Content.ReadAsStringAsync();
                    var jsonResult = JsonConvert.DeserializeObject<StockQuoteResponseContainer>(jsonString);

                    if (jsonResult.Response.Symbol == null)
                        return null;

                    return jsonResult.Response;
                }
                catch (JsonException)
                {
                    return null;
                }
            }
        }

        private class StockQuoteResponseContainer
        {
            [JsonProperty("Global Quote")]
            public StockQuoteResponse Response;
        }

        public class StockQuoteResponse
        {
            [JsonProperty("01. symbol")] public string Symbol { get; private set; }

            [JsonProperty("02. open")] public decimal Open { get; private set; }
            [JsonProperty("03. high")] public decimal High { get; private set; }
            [JsonProperty("04. low")] public decimal Low { get; private set; }
            [JsonProperty("05. price")] public decimal Price { get; private set; }

            [JsonProperty("06. volume")] public long Volume { get; private set; }

            [JsonProperty("09. change")] public decimal Change { get; private set; }
        }
    }
}
