using System.Threading.Tasks;
using Mute.Moe.Discord.Modules.Payment;
using Mute.Moe.Discord.Services.Users;

namespace Mute.Moe.Services.Payment;

/// <summary>
/// Store for pending transaction - i.e. transactions that have been created but need confirming before conversion into an actual transaction
/// </summary>
public interface IPendingTransactions
{
    /// <summary>
    /// Create a new pending transaction
    /// </summary>
    /// <param name="fromId"></param>
    /// <param name="toId"></param>
    /// <param name="amount"></param>
    /// <param name="unit"></param>
    /// <param name="note"></param>
    /// <param name="instant"></param>
    /// <returns></returns>
    Task<uint> CreatePending(ulong fromId, ulong toId, decimal amount,  string unit, string? note, DateTime instant);

    /// <summary>
    /// Query for a transaction
    /// </summary>
    /// <param name="debtId"></param>
    /// <param name="state"></param>
    /// <param name="fromId"></param>
    /// <param name="toId"></param>
    /// <param name="unit"></param>
    /// <param name="after"></param>
    /// <param name="before"></param>
    /// <returns></returns>
    IAsyncEnumerable<IPendingTransaction> Get(uint? debtId = null, PendingState? state = null, ulong? fromId = null, ulong? toId = null, string? unit = null, DateTime? after = null, DateTime? before = null);

    /// <summary>
    /// Confirm a pending transaction
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<ConfirmResult> ConfirmPending(uint id);

    /// <summary>
    /// Dent a pending transaction
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<DenyResult> DenyPending(uint id);
}

/// <summary>
/// Possible results from <see cref="IPendingTransactions.ConfirmPending"/>
/// </summary>
public enum ConfirmResult
{
    /// <summary>
    /// Transaction has been successfully confirmed
    /// </summary>
    Confirmed,

    /// <summary>
    /// Confirmation failed - transaction had already been denied
    /// </summary>
    AlreadyDenied,

    /// <summary>
    /// Confirmation failed - transaction had already been confirmed
    /// </summary>
    AlreadyConfirmed,

    /// <summary>
    /// Confirmation failed - transaction did not exist
    /// </summary>
    IdNotFound,
}

/// <summary>
/// Possible results from <see cref="IPendingTransactions.DenyPending"/>
/// </summary>
public enum DenyResult
{
    /// <summary>
    /// Transaction has been successfully denied
    /// </summary>
    Denied,

    /// <summary>
    /// Deny failed - transaction had already been denied
    /// </summary>
    AlreadyDenied,

    /// <summary>
    /// Deny failed - transaction had already been confirmed
    /// </summary>
    AlreadyConfirmed,

    /// <summary>
    /// Deny failed - transaction did not exist
    /// </summary>
    IdNotFound,
}

/// <summary>
/// Possible states of a pending transaction
/// </summary>
public enum PendingState
{
    /// <summary>
    /// Transaction is pending and needs to be confirmed or denied
    /// </summary>
    Pending,

    /// <summary>
    /// Transaction has been confirmed
    /// </summary>
    Confirmed,

    /// <summary>
    /// Transaction has been denied
    /// </summary>
    Denied,
}

/// <summary>
/// Represents A transaction which has not been confirmed.
/// </summary>
public interface IPendingTransaction
{
    /// <summary>
    /// Unique ID of this pending transaction
    /// </summary>
    uint Id { get; }

    /// <summary>
    /// Source user
    /// </summary>
    ulong FromId { get; }

    /// <summary>
    /// Destination user
    /// </summary>
    ulong ToId { get; }

    /// <summary>
    /// Amount in transaction
    /// </summary>
    decimal Amount { get; }

    /// <summary>
    /// Instant of transaction
    /// </summary>
    DateTime Instant { get; }

    /// <summary>
    /// Unit of transaction
    /// </summary>
    string Unit { get; }

    /// <summary>
    /// Human readable note attached to transaction
    /// </summary>
    string? Note { get; }

    /// <summary>
    /// Current state of this pending transaction
    /// </summary>
    PendingState State { get; }
}

/// <summary>
/// Extensions for <see cref="IPendingTransaction"/>
/// </summary>
public static class IPendingTransactionExtensions
{
    /// <summary>
    /// String format a pending transaction
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="users"></param>
    /// <param name="mention"></param>
    /// <returns></returns>
    public static Task<string> Format(this IPendingTransaction transaction, IUserService users, bool mention = false)
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

internal class PendingTransaction
    : IPendingTransaction
{
    public ulong FromId { get; }
    public ulong ToId { get; }
    public decimal Amount { get; }
    public DateTime Instant { get; }

    public string Unit { get; }
    public string? Note { get; }

    public PendingState State { get; }
    public uint Id { get; }

    public PendingTransaction(ulong fromId, ulong toId, decimal amount,  string unit, string? note, DateTime instant, PendingState state, uint id)
    {
        FromId = fromId;
        ToId = toId;
        Amount = amount;
        Unit = unit;
        Note = note;
        Instant = instant;
        State = state;
        Id = id;
    }
}