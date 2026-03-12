using System.Threading.Tasks;
using Mute.Moe.Discord.Modules.Payment;
using Mute.Moe.Discord.Services.Users;


namespace Mute.Moe.Services.Payment;

/// <summary>
/// Represents A transaction of some amount of a thing from one person to another
/// </summary>
/// <param name="FromId">ID of the source user</param>
/// <param name="ToId">ID of the destination user</param>
/// <param name="Amount">Amount transferred</param>
/// <param name="Unit">Unit of transaction (e.g. GBP)</param>
/// <param name="Note">A human readable note attached to this transaction</param>
/// <param name="Instant">Exact time of transaction</param>
public sealed record Transaction(ulong FromId, ulong ToId, decimal Amount, string Unit, string? Note, DateTime Instant)
{
    /// <summary>
    /// Convert a transaction into a string
    /// </summary>
    /// <param name="users"></param>
    /// <param name="mention"></param>
    /// <returns></returns>
    public Task<string> Format(IUserService users, bool mention = false)
    {
        return TransactionFormatting.FormatTransaction(
            users,
            FromId,
            ToId,
            Note,
            Instant,
            Amount,
            Unit,
            mention
        );
    }
}