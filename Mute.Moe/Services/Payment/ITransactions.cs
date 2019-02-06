using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using System.Linq;
using Mute.Moe.AsyncEnumerable.Extensions;
using Mute.Moe.Extensions;

namespace Mute.Moe.Services.Payment
{
    public static class ITransactionsExtensions
    {
        /// <summary>
        /// Convert a set of transactions into balances
        /// </summary>
        /// <param name="primaryUser"></param>
        /// <param name="transactions"></param>
        [NotNull, ItemNotNull] private static async Task<IEnumerable<IBalance>> TransactionsToBalances(ulong primaryUser, [NotNull] IAsyncEnumerable<ITransaction> transactions)
        {
            // Accumulate a lookup table of user -> unit -> amount
            //user in this case is always the secondary user (the other is implicitly the primary user)
            var accumulator = new Dictionary<ulong, Dictionary<string, decimal>>();

            Dictionary<string, decimal> GetInner(ulong user)
            {
                if (!accumulator.TryGetValue(user, out var lookup))
                {
                    lookup = new Dictionary<string, decimal>();
                    accumulator[user] = lookup;
                }

                return lookup;
            }

            void Add(IDictionary<string, decimal> lookup, string unit, decimal add)
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
        [ItemNotNull] public static async Task<IEnumerable<IBalance>> GetBalances([NotNull] this ITransactions database, ulong userA, ulong? userB, string unit = null)
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
        [ItemNotNull] public static async Task<IEnumerable<ITransaction>> GetAllTransactions([NotNull] this ITransactions database, ulong a, ulong? b)
        {
            var ab = await database.GetTransactions(fromId: a, toId: b);
            var ba = await database.GetTransactions(fromId: b, toId: a);

            return await ab.Concat(ba).OrderBy(t => t.Instant).ToArray();
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
        [NotNull] Task CreateTransaction(ulong fromId, ulong toId, decimal amount, [NotNull] string unit, [CanBeNull] string note, DateTime instant);

        /// <summary>
        /// Get all transactions, optionally filtered by source, sink, unit and time range
        /// </summary>
        /// <returns></returns>
        [NotNull, ItemNotNull] Task<IAsyncEnumerable<ITransaction>> GetTransactions(ulong? fromId = null, ulong? toId = null, string unit = null, DateTime? after = null, DateTime? before = null);
    }
}
