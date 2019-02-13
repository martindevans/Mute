using JetBrains.Annotations;

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
        [NotNull] string Unit { get; }

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

    internal class Balance
        : IBalance
    {
        public string Unit { get; }
        public ulong UserA { get; }
        public ulong UserB { get; }
        public decimal Amount { get; }

        public Balance([NotNull] string unit, ulong a, ulong b, decimal amount)
        {
            Unit = unit;
            UserA = a;
            UserB = b;
            Amount = amount;
        }
    }
}
