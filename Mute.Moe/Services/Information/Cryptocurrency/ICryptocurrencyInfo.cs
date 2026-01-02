using System.Threading.Tasks;
using Mute.Moe.Tools;
using Mute.Moe.Tools.Providers;


namespace Mute.Moe.Services.Information.Cryptocurrency;

/// <summary>
/// Get general info about cryptocurrencies
/// </summary>
public interface ICryptocurrencyInfo
{
    /// <summary>
    /// Find a cryptocurrency by symbol (e.g. BTC)
    /// </summary>
    /// <param name="symbol"></param>
    /// <returns></returns>
    Task<ICurrency?> FindBySymbol(string symbol);

    /// <summary>
    /// Find a cryptocurrency by name (e.g. Bitcoin)
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    Task<ICurrency?> FindByName(string name);

    /// <summary>
    /// Find a cryptocurrency by name (e.g. BTC or Bitcoin)
    /// </summary>
    /// <param name="symbolOrName"></param>
    /// <returns></returns>
    Task<ICurrency?> FindBySymbolOrName(string symbolOrName);

    /// <summary>
    /// Get a ticker price for the given cryptocurrency
    /// </summary>
    /// <param name="currency"></param>
    /// <returns></returns>
    Task<ITicker?> GetTicker(ICurrency currency);
}

/// <summary>
/// A single price quote
/// </summary>
public interface IQuote
{
    /// <summary>
    /// The price
    /// </summary>
    decimal Price { get; }

    /// <summary>
    /// Volume traded in 24 hours
    /// </summary>
    decimal? Volume24H { get; }

    /// <summary>
    /// Change in price over the last hour
    /// </summary>
    decimal? PctChange1H { get; }

    /// <summary>
    /// Change in price over the last 24 hours
    /// </summary>
    decimal? PctChange24H { get; }

    /// <summary>
    /// Change in price over the last 7 days
    /// </summary>
    decimal? PctChange7D { get; }
}

/// <summary>
/// Represents price information about a currency
/// </summary>
public interface ITicker
{
    /// <summary>
    /// The currency this ticker is for
    /// </summary>
    ICurrency Currency { get; }

    /// <summary>
    /// The approximate number of coins circulating for this cryptocurrency.
    /// </summary>
    decimal? CirculatingSupply { get; }

    /// <summary>
    /// The approximate total amount of coins in existence right now (minus any coins that have been verifiably burned).
    /// </summary>
    decimal? TotalSupply { get; }

    /// <summary>
    /// The expected maximum limit of coins ever to be available for this cryptocurrency.
    /// </summary>
    decimal? MaxSupply { get; }

    /// <summary>
    /// Price quotes, mapping from Symbol => quote for price in that other symbol
    /// </summary>
    IReadOnlyDictionary<string, IQuote> Quotes { get; }
}

/// <summary>
/// A currency
/// </summary>
public interface ICurrency
{
    /// <summary>
    /// Unique ID of this currency
    /// </summary>
    uint Id { get; }

    /// <summary>
    /// Human readable name of this currency
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Short symbol of this currency
    /// </summary>
    string Symbol { get; }
}

/// <summary>
/// Provides cryptocurrency related tools
/// </summary>
public class CryptocurrencyInfoToolProvider
    : IToolProvider
{
    private readonly ICryptocurrencyInfo _info;

    /// <inheritdoc />
    public IReadOnlyList<ITool> Tools { get; }

    /// <summary>
    /// Create a new <see cref="CryptocurrencyInfoToolProvider"/>
    /// </summary>
    /// <param name="info"></param>
    public CryptocurrencyInfoToolProvider(ICryptocurrencyInfo info)
    {
        _info = info;

        Tools =
        [
            new AutoTool("get_cryptocurrency_info", false, GetInfo)
        ];
    }

    /// <summary>
    /// Given a cryptocurrency name (e.g. Bitcoin) or a ticker symbol (e.g. BTC) retrieve price information about the currency, including ticker in various other currencies.
    /// </summary>
    /// <param name="query">The name or symbol to query</param>
    /// <returns></returns>
    private async Task<object> GetInfo(string query)
    {
        var currency = await _info.FindBySymbolOrName(query);
        if (currency == null)
            return new { error = "Cannot find cryptocurrency with that name or symbol" };

        var ticker = await _info.GetTicker(currency);

        return new CryptoInfo(currency, ticker);
    }

    private record CryptoInfo(ICurrency Currency, ITicker? Ticker);
}