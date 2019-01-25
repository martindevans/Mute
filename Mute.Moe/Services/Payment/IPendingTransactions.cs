using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Mute.Moe.Services.Payment
{
    public interface IPendingTransactions
    {
        [NotNull] Task<uint> CreatePending(ulong fromId, ulong toId, decimal amount, [NotNull] string unit, [CanBeNull] string note, DateTime instant);

        [NotNull, ItemNotNull] Task<IAsyncEnumerable<IPendingTransaction>> Get(uint? debtId = null, PendingState? state = null, ulong? fromId = null, ulong? toId = null, [CanBeNull] string unit = null, DateTime? after = null, DateTime? before = null);

        [NotNull] Task<ConfirmResult> ConfirmPending(uint id);

        [NotNull] Task<DenyResult> DenyPending(uint id);
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

        [NotNull] string Unit { get; }
        [CanBeNull] string Note { get; }

        PendingState State { get; }
    }

    internal class PendingTransaction
        : IPendingTransaction
    {
        public ulong FromId { get; }
        public ulong ToId { get; }
        public decimal Amount { get; }
        public DateTime Instant { get; }

        public string Unit { get; }
        public string Note { get; }

        public PendingState State { get; }
        public uint Id { get; }

        public PendingTransaction(ulong fromId, ulong toId, decimal amount, [NotNull] string unit, [CanBeNull] string note, DateTime instant, PendingState state, uint id)
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
}
