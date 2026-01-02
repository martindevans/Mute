using System.Threading.Tasks;
using Mute.Moe.Discord.Modules.Payment;
using Mute.Moe.Discord.Services.Users;


namespace Mute.Moe.Services.Payment;

/// <summary>
/// Represents A transaction of some amount of a thing from one person to another
/// </summary>
public interface ITransaction
{
    /// <summary>
    /// ID of the source user
    /// </summary>
    ulong FromId { get; }

    /// <summary>
    /// ID of the destination user
    /// </summary>
    ulong ToId { get; }

    /// <summary>
    /// Amount transferred
    /// </summary>
    decimal Amount { get; }

    /// <summary>
    /// Exact time of transaction
    /// </summary>
    DateTime Instant { get; }

    /// <summary>
    /// Unit of transaction (e.g. GBP)
    /// </summary>
    string Unit { get; }

    /// <summary>
    /// A human readable note attached to this transaction
    /// </summary>
    string? Note { get; }
}

/// <summary>
/// Extensions to <see cref="ITransaction"/>
/// </summary>
public static class ITransactionExtensions
{
    /// <summary>
    /// Convert a transaction into a string
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="users"></param>
    /// <param name="mention"></param>
    /// <returns></returns>
    public static Task<string> Format(this ITransaction transaction, IUserService users, bool mention = false)
    {
        return TransactionFormatting.FormatTransaction(
            users,
            transaction.FromId,
            transaction.ToId,
            transaction.Note,
            transaction.Instant,
            transaction.Amount,
            transaction.Unit,
            mention
        );
    }
}

internal record Transaction(ulong FromId, ulong ToId, decimal Amount, string Unit, string? Note, DateTime Instant)
    : ITransaction;