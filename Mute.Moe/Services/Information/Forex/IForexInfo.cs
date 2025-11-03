using Mute.Moe.Tools;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Information.Forex;

public interface IForexInfo
{
    Task<IForexQuote?> GetExchangeRate(string fromSymbol, string toSymbol);
}

public interface IForexQuote
{
    string FromCode { get; }
    string FromName { get; }

    string ToCode { get; }
    string ToName { get; }

    decimal ExchangeRate { get; }
}

/// <summary>
/// Provides foreign exchange related tools
/// </summary>
public class ForexToolProvider
    : IToolProvider
{
    private readonly IForexInfo _info;

    /// <inheritdoc />
    public IReadOnlyList<ITool> Tools { get; }

    /// <summary>
    /// Create a new <see cref="ForexToolProvider"/>
    /// </summary>
    /// <param name="info"></param>
    public ForexToolProvider(IForexInfo info)
    {
        _info = info;

        Tools =
        [
            new AutoTool("get_currency_exchange_rate", false, GetInfo)
        ];
    }

    /// <summary>
    /// Given a currency symbol (e.g. GBP) and another currency symbol (USD) return information about the change rate from the first to the second.
    /// </summary>
    /// <param name="from">Currency symbol to convert from</param>
    /// <param name="to">Currency symbol to convert to</param>
    /// <returns></returns>
    private async Task<object> GetInfo(string from, string to)
    {
        var quote = await _info.GetExchangeRate(from, to);
        if (quote == null)
            return new { error = "Cannot find exchange rate for this symbol pair" };

        return quote;
    }
}