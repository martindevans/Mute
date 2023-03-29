using System;
using System.Threading.Tasks;
using Mute.Moe.Discord.Services.Users;
using Mute.Moe.Extensions;

namespace Mute.Moe.Discord.Modules.Payment;

public static class TransactionFormatting
{
    public static string FormatCurrency(decimal amount, string unit)
    {
        var sym = unit.TryGetCurrencySymbol();

        return unit == sym
             ? $"{amount}({unit.ToUpperInvariant()})"
             : $"{sym}{amount}";
    }

    public static async Task<string> FormatTransaction(IUserService users, ulong from, ulong to, string? note, DateTime instant, decimal amount, string unit, bool mention = false)
    {
        var fromName = await users.Name(from, mention: mention);
        var toName = await users.Name(to, mention: mention);
        var noteFormat = string.IsNullOrWhiteSpace(note) ? "" : $"'{note}'";

        return $"[{instant:HH\\:mm UTC dd-MMM-yyyy}] {FormatCurrency(amount, unit)} {fromName} => {toName} {noteFormat}";
    }
}