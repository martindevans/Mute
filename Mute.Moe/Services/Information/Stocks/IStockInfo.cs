using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Mute.Moe.Services.Information.Stocks
{
    public interface IStockInfo
    {
        [ItemCanBeNull] Task<IStockQuote> GetQuote(string symbol);
    }

    public interface IStockQuote
    {
        string Symbol { get; }

        decimal Open { get; }
        decimal High { get; }
        decimal Low { get; }
        decimal Price { get; }

        long Volume { get; }
    }
}
