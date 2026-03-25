using System.Threading.Tasks;
using Mute.Moe.Discord.Modules.Payment;
using Mute.Moe.Discord.Services.Users;

namespace Mute.Moe.Services.Payment;

/// <summary>
/// The balance of payments between 2 users
/// </summary>
/// <param name="Unit">Unit of account</param>
/// <param name="UserA">One user</param>
/// <param name="UserB">Other user</param>
/// <param name="Amount">Amount that B owes A. May be negative, in which case lender and borrower are reversed.</param>
public record Balance(string Unit, ulong UserA, ulong UserB, decimal Amount)
{
    /// <summary>
    /// Convert a balance between 2 users into a string
    /// </summary>
    /// <param name="users"></param>
    /// <returns></returns>
    public async Task<string> Format(IUserService users)
    {
        var a = await users.Name(UserA);
        var b = await users.Name(UserB);
        var (borrower, lender) = Amount < 0
                               ? (a, b)
                               : (b, a);

        var currency = TransactionFormatting.FormatCurrency(Math.Abs(Amount), Unit);
        return $"{borrower} owes {currency} to {lender}";
    }
}