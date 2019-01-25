using JetBrains.Annotations;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Information.Forex
{
    public interface IForexInfo
    {
        [ItemCanBeNull] Task<IForexQuote> GetExchangeRate(string fromSymbol, string toSymbol);
    }

    public interface IForexQuote
    {
        string FromCode { get; }
        string FromName { get; }

        string ToCode { get; }
        string ToName { get; }

        decimal ExchangeRate { get; }
    }
}
