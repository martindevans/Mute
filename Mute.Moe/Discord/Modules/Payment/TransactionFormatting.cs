using System.Threading.Tasks;
using Mute.Moe.Discord.Services.Users;

namespace Mute.Moe.Discord.Modules.Payment;

/// <summary>
/// Format payment transactions into strings
/// </summary>
public static class TransactionFormatting
{
    /// <summary>
    /// Format a currency and an amount. e.g. (10, "GBP") => "£10"
    /// </summary>
    /// <param name="amount"></param>
    /// <param name="unit"></param>
    /// <returns></returns>
    public static string FormatCurrency(decimal amount, string unit)
    {
        var sym = unit.TryGetCurrencySymbol();

        return unit == sym
             ? $"{amount}({unit.ToUpperInvariant()})"
             : $"{sym}{amount}";
    }

    /// <summary>
    /// Format a transaction from one user to another
    /// </summary>
    /// <param name="users"></param>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="note"></param>
    /// <param name="instant"></param>
    /// <param name="amount"></param>
    /// <param name="unit"></param>
    /// <param name="mention"></param>
    /// <returns></returns>
    public static async Task<string> FormatTransaction(IUserService users, ulong from, ulong to, string? note, DateTime instant, decimal amount, string unit, bool mention = false)
    {
        var fromName = await users.Name(from, mention: mention);
        var toName = await users.Name(to, mention: mention);
        var noteFormat = string.IsNullOrWhiteSpace(note) ? "" : $"'{note}'";

        return $"[{instant:HH\\:mm UTC dd-MMM-yyyy}] {FormatCurrency(amount, unit)} {fromName} => {toName} {noteFormat}";
    }
}