using Mute.Moe.Tools;
using System.Threading.Tasks;
using HandyAgentFramework;
using IToolProvider = Mute.Moe.Tools.Providers.IToolProvider;

namespace Mute.Moe.Services.Information.Forex;

/// <summary>
/// Get currency foreign exchange info
/// </summary>
public interface IForexInfo
{
    /// <summary>
    /// Get the exchange rate, converting from one currency to another
    /// </summary>
    /// <param name="fromSymbol">Symbol to convert from</param>
    /// <param name="toSymbol">Symbol to convert to</param>
    /// <returns></returns>
    Task<IForexQuote?> GetExchangeRate(string fromSymbol, string toSymbol);
}

/// <summary>
/// A foreign exchange quote
/// </summary>
public interface IForexQuote
{
    /// <summary>
    /// Currency code converting from
    /// </summary>
    string FromCode { get; }

    /// <summary>
    /// Currency name converting from
    /// </summary>
    string FromName { get; }

    /// <summary>
    /// Currency code converting to
    /// </summary>
    string ToCode { get; }

    /// <summary>
    /// Currency name converting to
    /// </summary>
    string ToName { get; }

    /// <summary>
    /// Exchange rate for this pair
    /// </summary>
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
    public IReadOnlyList<ToolDefinition> Tools { get; }

    /// <summary>
    /// Create a new <see cref="ForexToolProvider"/>
    /// </summary>
    /// <param name="info"></param>
    public ForexToolProvider(IForexInfo info)
    {
        _info = info;

        Tools =
        [
            new DocStringTool(ToolGroups.Info.Currency, "get_forex", GetInfo)
        ];
    }

    /// <summary>
    /// Given a currency symbol (e.g. GBP) and another currency symbol (USD) return information about the currency exchange rate from the first to the second.
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