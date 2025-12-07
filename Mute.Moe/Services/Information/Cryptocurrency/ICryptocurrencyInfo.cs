using System.Threading.Tasks;
using Mute.Moe.Tools;
using Mute.Moe.Tools.Providers;


namespace Mute.Moe.Services.Information.Cryptocurrency;

public interface ICryptocurrencyInfo
{
    Task<ICurrency?> FindBySymbol(string symbol);

    Task<ICurrency?> FindByName(string name);

    Task<ICurrency?> FindBySymbolOrName(string symbolOrName);

    Task<ICurrency?> FindById(uint id);

    Task<ITicker?> GetTicker(ICurrency currency);
}

public interface IQuote
{
    decimal Price { get; }
    decimal? Volume24H { get; }
    decimal? PctChange1H { get; }
    decimal? PctChange24H { get; }
    decimal? PctChange7D { get; }
}

/// <summary>
/// Represents information about a currency
/// </summary>
public interface ITicker
{
    ICurrency Currency { get; }

    decimal? CirculatingSupply { get; }
    decimal? TotalSupply { get; }
    decimal? MaxSupply { get; }

    /// <summary>
    /// Price quotes, mapping from Symbol => quote for price in that other symbol
    /// </summary>
    IReadOnlyDictionary<string, IQuote> Quotes { get; }
}

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