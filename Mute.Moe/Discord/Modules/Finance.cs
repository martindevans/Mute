using System;
using System.Globalization;
using System.Threading.Tasks;
using Discord.Commands;
using JetBrains.Annotations;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Extensions;
using Mute.Moe.Services.Information.Cryptocurrency;
using Mute.Moe.Services.Information.Forex;
using Mute.Moe.Services.Information.Stocks;

namespace Mute.Moe.Discord.Modules
{
    [ThinkingReply]
    public class Finance
        : BaseModule
    {
        private readonly ICryptocurrencyInfo _crypto;
        private readonly IStockInfo _stocks;
        private readonly IForexInfo _forex;

        public Finance(ICryptocurrencyInfo crypto, IStockInfo stocks, IForexInfo forex)
        {
            _crypto = crypto;
            _stocks = stocks;
            _forex = forex;
        }

        [Command("ticker"), Summary("I will find out information about a stock or currency")]
        public async Task Ticker([NotNull] string symbolOrName, string quote = "USD")
        {
            try
            {
                if (await TickerAsCrypto(symbolOrName, quote))
                    return;

                if (await TickerAsStock(symbolOrName))
                    return;

                if (await TickerAsForex(symbolOrName, quote))
                    return;

                await TypingReplyAsync($"I can't find a stock or a currency called '{symbolOrName}'");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private async Task<bool> TickerAsStock(string symbolOrName)
        {
            var result = await _stocks.GetQuote(symbolOrName);

            if (result != null)
            {
                var change = "";
                var delta = result.Price - result.Open;
                if (delta > 0)
                    change += "up ";
                else
                    change += "down";
                change += $"{(delta / result.Price):P}";

                await TypingReplyAsync($"{result.Symbol} is trading at {result.Price:0.00}, {change} since opening today");
                return true;
            }

            return false;
        }

        private async Task<bool> TickerAsForex(string symbolOrName, string quote)
        {
            var result = await _forex.GetExchangeRate(symbolOrName, quote);

            if (result != null)
            {
                await TypingReplyAsync($"{result.FromName} ({result.FromCode}) is worth {result.ToCode.TryGetCurrencySymbol()}{result.ExchangeRate.ToString("0.00", CultureInfo.InvariantCulture)}");
                return true;
            }

            return false;
        }

        private async Task<bool> TickerAsCrypto([NotNull] string symbolOrName, string quote)
        {
            //Try to parse the sym/name as a cryptocurrency
            var currency = await _crypto.FindBySymbolOrName(symbolOrName);
            if (currency == null)
                return false;

            var ticker = await _crypto.GetTicker(currency, quote);

            string reply;
            if (ticker == null)
            {
                reply = $"{currency.Name} ({currency.Symbol}) doesn't seem to have any price information associated with it";
            }
            else
            {
                reply = $"{ticker.Currency.Name} ({ticker.Currency.Symbol})";

                //Try to find quote in selected currency, if not then default to USD
                if (!ticker.Quotes.TryGetValue(quote.ToUpperInvariant(), out var val) && quote != "USD")
                {
                    await TypingReplyAsync($"I'm not sure what the value is in '{quote.ToUpperInvariant()}', I'll try 'USD' instead");

                    quote = "USD";
                    ticker.Quotes.TryGetValue(quote, out val);
                }

                //Format the value part of the quote
                if (val != null)
                {
                    var price = val.Price.ToString("0.00", CultureInfo.InvariantCulture);
                    reply += $" is worth {quote.TryGetCurrencySymbol().ToUpperInvariant()}{price}";

                    if (val.PctChange24H.HasValue)
                    {
                        if (val.PctChange24H > 0)
                            reply += " (up";
                        else
                            reply += " (down";

                        reply += $" {val.PctChange24H}% in 24H)";
                    }
                }
            }

            await TypingReplyAsync(reply);
            return true;
        }
    }
}
