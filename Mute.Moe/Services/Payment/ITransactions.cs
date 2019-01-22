using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using System.Linq;
using Mute.Moe.Extensions;

namespace Mute.Moe.Services.Payment
{
    public static class ITransactionsExtensions
    {
        /// <summary>
        /// Convert a set of transactions into balances
        /// </summary>
        /// <param name="transactions"></param>
        private static async Task<IEnumerable<IBalance>> TransactionsToBalances(ulong primaryUser, IAsyncEnumerable<ITransaction> transactions)
        {
            // Accumulate a lookup table of user -> unit -> amount
            //user in this case is always the secondary user (the other is implicitly the primary user)
            Dictionary<ulong, Dictionary<string, decimal>> accumulator = new Dictionary<ulong, Dictionary<string, decimal>>();

            Dictionary<string, decimal> GetInner(ulong user)
            {
                if (!accumulator.TryGetValue(user, out var lookup))
                {
                    lookup = new Dictionary<string, decimal>();
                    accumulator[user] = lookup;
                }

                return lookup;
            }

            void Add(Dictionary<string, decimal> lookup, string unit, decimal add)
            {
                lookup.TryGetValue(unit, out var amount);
                lookup[unit] = amount + add;
            }

            await transactions.EnumerateAsync(async transaction => {
                Dictionary<string, decimal> inner;
                if (transaction.FromId == primaryUser)
                    inner = GetInner(transaction.ToId);
                else if (transaction.ToId == primaryUser)
                    inner = GetInner(transaction.FromId);
                else
                    return;

                bool positive = transaction.FromId == primaryUser;
                Add(inner, transaction.Unit, transaction.Amount * (positive ? 1 : -1));
            });

            //Create a list of all results
            var results = new List<IBalance>();
            foreach (var (user, inner) in accumulator)
                foreach (var (unit, amount) in inner)
                    results.Add(new Balance(unit, primaryUser, user, amount));

            //Remove useless result
            results.RemoveAll(r => r.Amount == 0);

            //Order sensibly
            results.Sort((a, b) => a.Amount.CompareTo(b.Amount));

            return results;
        }

        /// <summary>
        /// Get the total balance of transactions between A and B (positive balance indicates that B owes A). If B is unspecified, get all balance involving user A and any other user
        /// </summary>
        /// <param name="database"></param>
        /// <param name="userA"></param>
        /// <param name="userB"></param>
        /// <returns>All non-zero balances in order of amount</returns>
        public static async Task<IEnumerable<IBalance>> GetBalances(this ITransactions database, ulong userA, ulong? userB, string unit = null)
        {
            //e.g.
            //A -> B £2
            //A -> B $3
            //B -> A £1
            //B -> A $5
            //Balance(A, B, £) = [ £1 ]
            //Balance(A, B, null) = [ £1, -$2 ]

            //Get transactions involving these two users in both directions
            var ab = await database.GetTransactions(userA, userB, unit);
            var ba = await database.GetTransactions(userB, userA, unit);

            //Convert to balances
            return await TransactionsToBalances(userA, ab.Concat(ba));
        }

        /// <summary>
        /// Get all transactions involving (up to) two users
        /// </summary>
        /// <param name="database"></param>
        /// <param name="a">One of the users in the transaction</param>
        /// <param name="b"></param>
        /// <returns>All transactions involving A (filtered to also involving B if specified), ordered by instant</returns>
        public static async Task<IEnumerable<ITransaction>> GetAllTransactions(this ITransactions database, ulong a, ulong? b)
        {
            var ab = await database.GetTransactions(fromId: a, toId: b);
            var ba = await database.GetTransactions(fromId: b, toId: a);

            return await ab.Concat(ba).OrderBy(t => t.Instant).ToArray();
        }
    }

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

    public class Balance
        : IBalance
    {
        public string Unit { get; }
        public ulong UserA { get; }
        public ulong UserB { get; }
        public decimal Amount { get; }

        public Balance(string unit, ulong a, ulong b, decimal amount)
        {
            Unit = unit;
            UserA = a;
            UserB = b;
            Amount = amount;
        }
    }

    public interface ITransactions
    {
        /// <summary>
        /// Create a transaction of an amount of a thing from one user to another
        /// </summary>
        /// <param name="fromId">Source user</param>
        /// <param name="toId">Sunk user</param>
        /// <param name="amount">Amount of thing</param>
        /// <param name="unit">Unit of thing (e.g. GBP)</param>
        /// <param name="note">A human readable note</param>
        /// <param name="instant">When this transaction happened</param>
        /// <returns></returns>
        Task CreateTransaction(ulong fromId, ulong toId, decimal amount, [NotNull] string unit, [CanBeNull] string note, DateTime instant);

        /// <summary>
        /// Get all transactions, optionally filtered by source, sink, unit and time range
        /// </summary>
        /// <returns></returns>
        Task<IAsyncEnumerable<ITransaction>> GetTransactions(ulong? fromId = null, ulong? toId = null, string unit = null, DateTime? after = null, DateTime? before = null);
    }

    /// <summary>
    /// Represents A transaction of some amount of a thing from one person to another
    /// </summary>
    public interface ITransaction
    {
        ulong FromId { get; }
        ulong ToId { get; }
        decimal Amount { get; }
        DateTime Instant { get; }

        [NotNull] string Unit { get; }
        [CanBeNull] string Note { get; }
    }

    internal class Transaction
        : ITransaction
    {
        public ulong FromId { get; }
        public ulong ToId { get; }
        public decimal Amount { get; }
        public DateTime Instant { get; }

        public string Unit { get; }
        public string Note { get; }

        public Transaction(ulong fromId, ulong toId, decimal amount, [NotNull] string unit, [CanBeNull] string note, DateTime instant)
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
