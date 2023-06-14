using System.Threading.Tasks;


namespace Mute.Moe.Services.Information.Cryptocurrency;

public interface ICryptocurrencyInfo
{
    Task<ICurrency?> FindBySymbol(string symbol);

    Task<ICurrency?> FindByName(string name);

    Task<ICurrency?> FindBySymbolOrName(string symbolOrName);

    Task<ICurrency?> FindById(uint id);

    Task<ITicker?> GetTicker(ICurrency currency, string? quote = null);
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