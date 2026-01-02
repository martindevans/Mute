using System.Threading.Tasks;

namespace Mute.Moe.Services.Information.Stocks;

/// <summary>
/// Search for stocks
/// </summary>
public interface IStockSearch
{
    /// <summary>
    /// Search for stocks by symbol, name etc
    /// </summary>
    /// <param name="search"></param>
    /// <returns></returns>
    IAsyncEnumerable<IStockSearchResult> Search(string search);

    /// <summary>
    /// Try to lookup a stock by symbol
    /// </summary>
    /// <param name="symbol"></param>
    /// <returns></returns>
    Task<IStockInfo?> Lookup(string symbol);
}

/// <summary>
/// Result from searching for a stock
/// </summary>
public interface IStockSearchResult
{
    /// <summary>
    /// Symbol (e.g. MSFT)
    /// </summary>
    string Symbol { get; }

    /// <summary>
    /// Name (e.g. Microsoft)
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Currency for this stock (e.g. USD)
    /// </summary>
    string Currency { get; }
}

/// <summary>
/// Info about a specific stock
/// </summary>
public interface IStockInfo
{
    /// <summary>
    /// Symbol (e.g. MSFT)
    /// </summary>
    string Symbol { get; }

    /// <summary>
    /// Name (e.g. Microsoft)
    /// </summary>
    string Name { get; }

    /// <summary>
    /// A description of the company this stock represents
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Currency for this stock (e.g. USD)
    /// </summary>
    string Currency { get; }
}