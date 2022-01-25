

using System;
using System.Threading.Tasks;
using Mute.Moe.Discord.Modules.Payment;
using Mute.Moe.Discord.Services.Users;

namespace Mute.Moe.Services.Payment
{
    /// <summary>
    /// An account balance between 2 users. A positive value indicates that B owes A.
    /// </summary>
    public interface IBalance
    {
        /// <summary>
        /// Unit this balance is in
        /// </summary>
         string Unit { get; }

        /// <summary>
        /// User on the receiving end of this balance
        /// </summary>
        ulong UserA { get; }

        /// <summary>
        /// User on the giving end of this balance
        /// </summary>
        ulong UserB { get; }

        /// <summary>
        /// Amount of the balance
        /// </summary>
        decimal Amount { get; }
    }

    public static class IBalanceExtensions
    {
        public static async Task<string> Format(this IBalance balance, IUserService users)
        {
            var a = await users.Name(balance.UserA);
            var b = await users.Name(balance.UserB);
            var (borrower, lender) = balance.Amount < 0 ? (a, b) : (b, a);

            var currency = TransactionFormatting.FormatCurrency(Math.Abs(balance.Amount), balance.Unit);
            return $"{borrower} owes {currency} to {lender}";
        }
    }

    internal class Balance
        : IBalance
    {
        public string Unit { get; }
        public ulong UserA { get; }
        public ulong UserB { get; }
        public decimal Amount { get; }

        public Balance( string unit, ulong a, ulong b, decimal amount)
        {
            Unit = unit;
            UserA = a;
            UserB = b;
            Amount = amount;
        }
    }
}
