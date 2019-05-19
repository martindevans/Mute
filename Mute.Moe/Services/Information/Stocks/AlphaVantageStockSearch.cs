using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Mute.Moe.AsyncEnumerable;
using Mute.Moe.Utilities;
using Newtonsoft.Json;

namespace Mute.Moe.Services.Information.Stocks
{
    public class AlphaVantageStockSearch
        : IStockSearch
    {
        private readonly IHttpClient _http;
        private readonly AlphaAdvantageConfig _config;

        public AlphaVantageStockSearch([NotNull] Configuration config, IHttpClient http)
        {
            _config = config.AlphaAdvantage;
            _http = http;
        }

        public async Task<IAsyncEnumerable<IStockSearchResult>> Search(string search)
        {
            //https://www.alphavantage.co/query?function=SYMBOL_SEARCH&keywords=g&apikey=demo

            using (var result = await _http.GetAsync($"https://www.alphavantage.co/query?function=SYMBOL_SEARCH&keywords={Uri.EscapeUriString(search)}&apikey={_config.Key}"))
            {
                if (!result.IsSuccessStatusCode)
                    return new EmptyAsyncEnumerable<IStockSearchResult>();

                StockSearchResponseContainer response;
                var serializer = new JsonSerializer();
                using (var sr = new StreamReader(await result.Content.ReadAsStreamAsync()))
                using (var jsonTextReader = new JsonTextReader(sr))
                    response = serializer.Deserialize<StockSearchResponseContainer>(jsonTextReader);

                return response.BestMatches.OrderByDescending(a => a.MatchScore).ToAsyncEnumerable();
            }
        }

        private class StockSearchResponseContainer
        {
            [JsonProperty("bestMatches"), UsedImplicitly]
            public StockSearchResponse[] BestMatches;
        }

        public class StockSearchResponse
            : IStockSearchResult
        {
            [JsonProperty("1. symbol"), UsedImplicitly] public string Symbol { get; private set; }

            [JsonProperty("2. name"), UsedImplicitly] public string Name { get; private set; }

            [JsonProperty("8. currency"), UsedImplicitly] public string Currency { get; private set; }

            [JsonProperty("9. matchScore"), UsedImplicitly] public string MatchScore { get; private set; }
        }
    }
}
