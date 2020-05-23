using System;

using Mute.Moe.Extensions;
using Mute.Moe.Services.Payment;

namespace Mute.Moe.Discord.Modules.Payment
{
    public static class TransactionFormatting
    {
         public static string FormatCurrency(decimal amount,  string unit)
        {
            var sym = unit.TryGetCurrencySymbol();

            if (unit == sym)
                return $"{amount}({unit.ToUpperInvariant()})";
            else
                return $"{sym}{amount}";
        }

         public static string FormatTransaction( BaseModule module,  ITransaction transaction, bool mention = false)
        {
            return FormatTransaction(module, transaction.FromId, transaction.ToId, transaction.Note, transaction.Instant, transaction.Amount, transaction.Unit, mention);
        }

         public static string FormatTransaction( BaseModule module,  IPendingTransaction transaction, bool mention = false)
        {
            return FormatTransaction(module, transaction.FromId, transaction.ToId, transaction.Note, transaction.Instant, transaction.Amount, transaction.Unit, mention);
        }

         public static string FormatTransaction( BaseModule module, ulong from, ulong to, string? note, DateTime instant, decimal amount,  string unit, bool mention = false)
        {
            var fromName = module.Name(from, mention);
            var toName = module.Name(to, mention);
            var noteFormat = string.IsNullOrWhiteSpace(note) ? "" : $"'{note}'";

            return $"[{instant:HH\\:mm UTC dd-MMM-yyyy}] {FormatCurrency(amount, unit)} {fromName} => {toName} {noteFormat}";
        }

         public static string FormatBalance( BaseModule module,  IBalance balance)
        {
            var a = module.Name(balance.UserA);
            var b = module.Name(balance.UserB);

            var (borrower, lender) = balance.Amount < 0 ? (a, b) : (b, a);

            return $"{borrower} owes {FormatCurrency(Math.Abs(balance.Amount), balance.Unit)} to {lender}";
        }
    }
}
