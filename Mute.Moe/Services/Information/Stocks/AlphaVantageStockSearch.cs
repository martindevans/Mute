using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable 0649 // Field not assigned
            [JsonProperty("bestMatches"), UsedImplicitly] private StockSearchResponse[]? _bestMatches;
#pragma warning restore 0649 // Field not assigned
#pragma warning restore IDE0044 // Add readonly modifier

            public StockSearchResponse[] BestMatches => _bestMatches ?? throw new InvalidOperationException("API returned null value for `bestMatches` field");
        }

        public class StockSearchResponse
            : IStockSearchResult
        {
#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable 0649 // Field not assigned
            [JsonProperty("1. symbol"), UsedImplicitly] private string? _symbol;
            [JsonProperty("2. name"), UsedImplicitly] private string? _name;
            [JsonProperty("8. currency"), UsedImplicitly] private string? _currency;
            [JsonProperty("9. matchScore"), UsedImplicitly] private string? _matchScore;
#pragma warning restore 0649 // Field not assigned
#pragma warning restore IDE0044 // Add readonly modifier

            public string Symbol => _symbol ?? throw new InvalidOperationException("API returned null value for `1. symbol` field");
            public string Name => _name ?? throw new InvalidOperationException("API returned null value for `2. name` field");
            public string Currency => _currency ?? throw new InvalidOperationException("API returned null value for `8. currency` field");
            public string MatchScore => _matchScore ?? throw new InvalidOperationException("API returned null value for `9. matchScore` field");
        }
    }
}
