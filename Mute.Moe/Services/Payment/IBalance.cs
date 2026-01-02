using System.Threading.Tasks;
using Mute.Moe.Discord.Modules.Payment;
using Mute.Moe.Discord.Services.Users;

namespace Mute.Moe.Services.Payment;

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

/// <summary>
/// Extensions to <see cref="IBalance"/>
/// </summary>
public static class IBalanceExtensions
{
    /// <summary>
    /// Convert a balance between 2 users into a string
    /// </summary>
    /// <param name="balance"></param>
    /// <param name="users"></param>
    /// <returns></returns>
    public static async Task<string> Format(this IBalance balance, IUserService users)
    {
        var a = await users.Name(balance.UserA);
        var b = await users.Name(balance.UserB);
        var (borrower, lender) = balance.Amount < 0 ? (a, b) : (b, a);

        var currency = TransactionFormatting.FormatCurrency(Math.Abs(balance.Amount), balance.Unit);
        return $"{borrower} owes {currency} to {lender}";
    }
}

internal record Balance(string Unit, ulong UserA, ulong UserB, decimal Amount)
    : IBalance;