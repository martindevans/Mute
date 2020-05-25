using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Mute.Moe.Services.Information.Stocks
{
    public class AlphaVantageStockSearch
        : IStockSearch
    {
        private readonly HttpClient _http;

        private readonly string _key;

        public AlphaVantageStockSearch(Configuration config, IHttpClientFactory http)
        {
            if (config.AlphaAdvantage == null)
                throw new ArgumentNullException(nameof(config.AlphaAdvantage));

            _key = config.AlphaAdvantage.Key ?? throw new ArgumentNullException(nameof(config.AlphaAdvantage.Key));
            _http = http.CreateClient();
        }

        public async IAsyncEnumerable<IStockSearchResult> Search(string search)
        {
            //https://www.alphavantage.co/query?function=SYMBOL_SEARCH&keywords=g&apikey=demo

            using var result = await _http.GetAsync($"https://www.alphavantage.co/query?function=SYMBOL_SEARCH&keywords={Uri.EscapeUriString(search)}&apikey={_key}");
            if (!result.IsSuccessStatusCode)
                yield break;

            StockSearchResponseContainer response;
            var serializer = new JsonSerializer();
            using (var sr = new StreamReader(await result.Content.ReadAsStreamAsync()))
            using (var jsonTextReader = new JsonTextReader(sr))
                response = serializer.Deserialize<StockSearchResponseContainer>(jsonTextReader)!;

            if (response?.BestMatches == null)
                yield break;

            foreach (var item in response.BestMatches.OrderByDescending(a => a.MatchScore))
                yield return item;
        }

        private class StockSearchResponseContainer
        {
            [JsonProperty("bestMatches"), UsedImplicitly]
            public StockSearchResponse[]? BestMatches;
        }

        public class StockSearchResponse
            : IStockSearchResult
        {
            [JsonProperty("1. symbol"), UsedImplicitly] public string? Symbol { get; private set; }
            [JsonProperty("2. name"), UsedImplicitly] public string? Name { get; private set; }
            [JsonProperty("8. currency"), UsedImplicitly] public string? Currency { get; private set; }
            [JsonProperty("9. matchScore"), UsedImplicitly] public string? MatchScore { get; private set; }
        }
    }
}
