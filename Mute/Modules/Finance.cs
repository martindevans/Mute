using System.Threading.Tasks;
using Discord.Commands;
using Mute.Extensions;
using Mute.Services;

namespace Mute.Modules
{
    public class Finance
        : ModuleBase
    {
        private readonly CryptoCurrencyService _cryptoService;
        private readonly IStockService _stockService;

        public Finance(CryptoCurrencyService cryptoService, IStockService stockService)
        {
            _cryptoService = cryptoService;
            _stockService = stockService;
        }

        [Command("ticker"), Summary("I will find out information about a stock or currency")]
        public async Task Ticker(string symbolOrName, string quote = "USD")
        {
            if (await TickerAsCrypto(symbolOrName, quote))
                return;

            if (await TickerAsStock(symbolOrName, quote))
                return;

            if (await TickerAsForex(symbolOrName, quote))
                return;

            await this.TypingReplyAsync($"I can't find a stock or a currency called '{symbolOrName}'");
        }

        private async Task<bool> TickerAsStock(string symbolOrName, string quote)
        {
            return false;
        }

        private async Task<bool> TickerAsForex(string symbolOrName, string quote)
        {
            return false;
        }

        private async Task<bool> TickerAsCrypto(string symbolOrName, string quote)
        {
            //Try to parse the sym/name as a cryptocurrency
            var currency = await _cryptoService.Find(symbolOrName);
            if (currency == null)
                return false;

            var ticker = await _cryptoService.GetTicker(currency, quote);

            //Begin forming the reply
            var reply = $"{ticker.Name} ({ticker.Symbol})";

            //Try to find quote in selected currency, if not then default to USD
            Task ongoingTask = null;
            if (!ticker.Quotes.TryGetValue(quote.ToUpperInvariant(), out var val) && quote != "USD")
            {
                ongoingTask = this.TypingReplyAsync($"I'm not sure what the value is in '{quote.ToUpperInvariant()}', I'll try 'USD' instead");

                quote = "USD";
                ticker.Quotes.TryGetValue(quote, out val);
            }

            //Format the value part of the quote
            if (val != null)
            {
                reply += $" is worth {quote.TryGetCurrencySymbol().ToUpperInvariant()}{val.Price:#} (";

                if (val.PctChange24H > 0)
                    reply += "up";
                else
                    reply += "down";

                reply += $" {val.PctChange24H}% in 24H)";
            }

            //If we were typing a previous response, wait for that to complete
            if (ongoingTask != null)
                await ongoingTask;

            await this.TypingReplyAsync(reply);

            return true;
        }
    }
}
