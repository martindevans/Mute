using System.Threading.Tasks;

namespace Mute.Moe.Services.Information.Stocks
{
    public interface IStockQuotes
    {
        Task<IStockQuote?> GetQuote(string symbol);
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
