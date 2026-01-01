using System.Threading.Tasks;

namespace Mute.Moe.Services.Payment;

/// <summary>
/// Extensions to get IOU transactions
/// </summary>
public static class ITransactionsExtensions
{
    /// <summary>
    /// Convert a set of transactions into balances
    /// </summary>
    /// <param name="primaryUser"></param>
    /// <param name="transactions"></param>
    private static async Task<IReadOnlyList<IBalance>> TransactionsToBalances(ulong primaryUser, IAsyncEnumerable<ITransaction> transactions)
    {
        // Accumulate a lookup table of user -> unit -> amount
        //user in this case is always the secondary user (the other is implicitly the primary user)
        var accumulator = new Dictionary<ulong, Dictionary<string, decimal>>();

        await foreach (var transaction in transactions)
        {
            Dictionary<string, decimal> inner;
            if (transaction.FromId == primaryUser)
                inner = GetInner(transaction.ToId);
            else if (transaction.ToId == primaryUser)
                inner = GetInner(transaction.FromId);
            else
                continue;

            var positive = transaction.FromId == primaryUser;
            Add(inner, transaction.Unit, transaction.Amount * (positive ? 1 : -1));
        }

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

        static void Add(IDictionary<string, decimal> lookup, string unit, decimal add)
        {
            lookup.TryGetValue(unit, out var amount);
            lookup[unit] = amount + add;
        }

        Dictionary<string, decimal> GetInner(ulong user)
        {
            if (!accumulator.TryGetValue(user, out var lookup))
            {
                lookup = new Dictionary<string, decimal>();
                accumulator[user] = lookup;
            }

            return lookup;
        }
    }

    /// <summary>
    /// Get the total balance of transactions between A and B (positive balance indicates that B owes A). If B is unspecified, get all balance involving user A and any other user
    /// </summary>
    /// <param name="database"></param>
    /// <param name="userA"></param>
    /// <param name="userB"></param>
    /// <param name="unit"></param>
    /// <returns>All non-zero balances in order of amount</returns>
    public static Task<IReadOnlyList<IBalance>> GetBalances(this ITransactions database, ulong userA, ulong? userB, string? unit = null)
    {
        //e.g.
        //A -> B £2
        //A -> B $3
        //B -> A £1
        //B -> A $5
        //Balance(A, B, £) = [ £1 ]
        //Balance(A, B, null) = [ £1, -$2 ]

        //Get transactions involving these two users in both directions
        var ab = database.GetTransactions(userA, userB, unit);
        var ba = database.GetTransactions(userB, userA, unit);

        //Convert to balances
        return TransactionsToBalances(userA, ab.Concat(ba));
    }

    /// <summary>
    /// Get all transactions involving (up to) two users
    /// </summary>
    /// <param name="database"></param>
    /// <param name="userA">One of the users in the transaction</param>
    /// <param name="userB"></param>
    /// <returns>All transactions involving A (filtered to also involving B if specified), ordered by instant</returns>
    public static async Task<IReadOnlyList<ITransaction>> GetAllTransactions(this ITransactions database, ulong userA, ulong? userB = null)
    {
        var ab = database.GetTransactions(fromId: userA, toId: userB);
        var ba = database.GetTransactions(fromId: userB, toId: userA);

        return await ab.Concat(ba).OrderByDescending(t => t.Instant).ToArrayAsync();
    }
}

/// <summary>
/// Service for storing transactions from one user to another
/// </summary>
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
    Task CreateTransaction(ulong fromId, ulong toId, decimal amount,  string unit, string? note, DateTime instant);

    /// <summary>
    /// Get all transactions, optionally filtered by source, sink, unit and time range
    /// </summary>
    /// <returns></returns>
    IAsyncEnumerable<ITransaction> GetTransactions(ulong? fromId = null, ulong? toId = null, string? unit = null, DateTime? after = null, DateTime? before = null);
}