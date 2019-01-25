using System;
using JetBrains.Annotations;
using Mute.Moe.Extensions;
using Mute.Moe.Services.Payment;

namespace Mute.Moe.Discord.Modules.Payment
{
    public static class TransactionFormatting
    {
        [NotNull] public static string FormatCurrency(decimal amount, [NotNull] string unit)
        {
            var sym = unit.TryGetCurrencySymbol();

            if (unit == sym)
                return $"{amount}({unit.ToUpperInvariant()})";
            else
                return $"{sym}{amount}";
        }

        [NotNull] public static string FormatTransaction([NotNull] BaseModule module, [NotNull] ITransaction transaction, bool mention = false)
        {
            return FormatTransaction(module, transaction.FromId, transaction.ToId, transaction.Note, transaction.Instant, transaction.Amount, transaction.Unit, mention);
        }

        [NotNull] public static string FormatTransaction([NotNull] BaseModule module, [NotNull] IPendingTransaction transaction, bool mention = false)
        {
            return FormatTransaction(module, transaction.FromId, transaction.ToId, transaction.Note, transaction.Instant, transaction.Amount, transaction.Unit, mention);
        }

        [NotNull] public static string FormatTransaction([NotNull] BaseModule module, ulong from, ulong to, [CanBeNull] string note, DateTime instant, decimal amount, [NotNull] string unit, bool mention = false)
        {
            var fromName = module.Name(from, mention);
            var toName = module.Name(to, mention);
            var noteFormat = string.IsNullOrWhiteSpace(note) ? "" : $"'{note}'";

            return $"[{instant:HH\\:mm UTC dd-MMM-yyyy}] {FormatCurrency(amount, unit)} {fromName} => {toName} {noteFormat}";
        }

        [NotNull] public static string FormatBalance([NotNull] BaseModule module, [NotNull] IBalance balance)
        {
            var a = module.Name(balance.UserA);
            var b = module.Name(balance.UserB);

            var (borrower, lender) = balance.Amount < 0 ? (a, b) : (b, a);

            return $"{borrower} owes {FormatCurrency(Math.Abs(balance.Amount), balance.Unit)} to {lender}";
        }
    }
}
