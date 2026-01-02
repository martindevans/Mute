using Mute.Moe.Tools;
using Mute.Moe.Tools.Providers;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Information.Stocks;

/// <summary>
/// Fetch stock price quotes
/// </summary>
public interface IStockQuotes
{
    /// <summary>
    /// Get a quote for a stock symbol (e.g. MSFT)
    /// </summary>
    /// <param name="symbol"></param>
    /// <returns></returns>
    Task<IStockQuote?> GetQuote(string symbol);
}

/// <summary>
/// A price quote for a stock
/// </summary>
public interface IStockQuote
{
    /// <summary>
    /// Symbol of the stock
    /// </summary>
    string Symbol { get; }

    /// <summary>
    /// Open price
    /// </summary>
    decimal Open { get; }

    /// <summary>
    /// High price
    /// </summary>
    decimal High { get; }

    /// <summary>
    /// Low price
    /// </summary>
    decimal Low { get; }

    /// <summary>
    /// Current price
    /// </summary>
    decimal Price { get; }

    /// <summary>
    /// Trading volume
    /// </summary>
    long Volume { get; }
}

/// <summary>
/// Provides stock price related tools
/// </summary>
public class StockToolProvider
    : IToolProvider
{
    private readonly IStockQuotes _quotes;
    private readonly IStockSearch _search;

    /// <inheritdoc />
    public IReadOnlyList<ITool> Tools { get; }

    /// <summary>
    /// Create a new <see cref="StockToolProvider"/>
    /// </summary>
    /// <param name="quotes"></param>
    /// <param name="search"></param>
    public StockToolProvider(IStockQuotes quotes, IStockSearch search)
    {
        _quotes = quotes;
        _search = search;

        Tools =
        [
            new AutoTool("get_stock_price", false, GetQuote),
            new AutoTool("get_stock_info", false, GetInfo),
            new AutoTool("search_for_stocks", false, SearchStocks),
        ];
    }

    /// <summary>
    /// Given a symbol (e.g. MSFT) get a stock price quote.
    /// </summary>
    /// <param name="symbol">The stock symbol to query</param>
    /// <returns></returns>
    private async Task<object> GetQuote(string symbol)
    {
        var quote = await _quotes.GetQuote(symbol);
        if (quote == null)
            return new { error = "Cannot find stock with symbol" };

        var info = await _search.Lookup(symbol);
        if (info == null)
            return new { error = "Cannot find stock with that symbol" };

        return new StockQuote(quote, info);
    }

    /// <summary>
    /// Given a symbol (e.g. MSFT) get general information about the company.
    /// </summary>
    /// <param name="symbol">The stock symbol to query</param>
    /// <returns></returns>
    private async Task<object> GetInfo(string symbol)
    {
        var info = await _search.Lookup("symbol");
        if (info == null)
            return new { error = "Cannot find stock with that symbol" };

        return info;
    }

    /// <summary>
    /// Search for stocks by name or symbol
    /// </summary>
    /// <param name="search">Term to search for (company name or symbol)</param>
    /// <param name="limit">Maximum number of results to return</param>
    /// <returns></returns>
    private async Task<object> SearchStocks(string search, int limit)
    {
        var arr = await _search.Search(search).Take(16).Take(limit).ToArrayAsync();
        return arr;
    }

    [UsedImplicitly]
    private record StockQuote(IStockQuote Quote, IStockInfo Info);
}