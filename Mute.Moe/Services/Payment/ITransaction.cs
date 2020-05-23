using System;


namespace Mute.Moe.Services.Payment
{
    /// <summary>
    /// Represents A transaction of some amount of a thing from one person to another
    /// </summary>
    public interface ITransaction
    {
        ulong FromId { get; }
        ulong ToId { get; }
        decimal Amount { get; }
        DateTime Instant { get; }

        string Unit { get; }
        string? Note { get; }
    }

    internal class Transaction
        : ITransaction
    {
        public ulong FromId { get; }
        public ulong ToId { get; }
        public decimal Amount { get; }
        public DateTime Instant { get; }

        public string Unit { get; }
        public string? Note { get; }

        public Transaction(ulong fromId, ulong toId, decimal amount,  string unit, string? note, DateTime instant)
        {
            FromId = fromId;
            ToId = toId;
            Amount = amount;
            Unit = unit;
            Note = note;
            Instant = instant;
        }
    }
}
