using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Mute.Moe.Services.Information.Cryptocurrency
{
    public interface ICryptocurrencyInfo
    {
        [ItemCanBeNull] Task<ICurrency> FindBySymbol([NotNull] string symbol);

        [ItemCanBeNull] Task<ICurrency> FindByName([NotNull] string name);

        [ItemCanBeNull] Task<ICurrency> FindBySymbolOrName([NotNull] string symbolOrName);

        [ItemCanBeNull] Task<ICurrency> FindById(uint id);

        [ItemCanBeNull] Task<ITicker> GetTicker([NotNull] ICurrency currency, [CanBeNull] string quote = null);
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
}
