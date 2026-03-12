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
    IAsyncEnumerable<PendingTransaction> Get(uint? debtId = null, PendingState? state = null, ulong? fromId = null, ulong? toId = null, string? unit = null, DateTime? after = null, DateTime? before = null);

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
/// <param name="FromId">Source user</param>
/// <param name="ToId">Destination user</param>
/// <param name="Amount">Amount in transaction</param>
/// <param name="Unit">Unit of transaction</param>
/// <param name="Note">Human readable note attached to transaction</param>
/// <param name="Instant">Instant of transaction</param>
/// <param name="State">Current state of this pending transaction</param>
/// <param name="Id">Unique ID of this pending transaction</param>
public sealed record PendingTransaction(ulong FromId, ulong ToId, decimal Amount, string Unit, string? Note, DateTime Instant, PendingState State, uint Id)
{
    /// <summary>
    /// String format a pending transaction
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