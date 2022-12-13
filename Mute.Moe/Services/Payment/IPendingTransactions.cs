using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mute.Moe.Discord.Modules.Payment;
using Mute.Moe.Discord.Services.Users;


namespace Mute.Moe.Services.Payment;

public interface IPendingTransactions
{
    Task<uint> CreatePending(ulong fromId, ulong toId, decimal amount,  string unit, string? note, DateTime instant);

    IAsyncEnumerable<IPendingTransaction> Get(uint? debtId = null, PendingState? state = null, ulong? fromId = null, ulong? toId = null, string? unit = null, DateTime? after = null, DateTime? before = null);

    Task<ConfirmResult> ConfirmPending(uint id);

    Task<DenyResult> DenyPending(uint id);
}

public enum ConfirmResult
{
    Confirmed,

    AlreadyDenied,
    AlreadyConfirmed,

    IdNotFound,
}

public enum DenyResult
{
    Denied,
        
    AlreadyDenied,
    AlreadyConfirmed,

    IdNotFound,
}

public enum PendingState
{
    Pending,

    Confirmed,

    Denied
}

/// <summary>
/// Represents A transaction which has not been confirmed.
/// </summary>
public interface IPendingTransaction
{
    uint Id { get; }

    ulong FromId { get; }
    ulong ToId { get; }
    decimal Amount { get; }
    DateTime Instant { get; }

    string Unit { get; }
    string? Note { get; }

    PendingState State { get; }
}

public static class IPendingTransactionExtensions
{
    public static async Task<string> Format(this IPendingTransaction transaction, IUserService users, bool mention = false)
    {
        return await TransactionFormatting.FormatTransaction(
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