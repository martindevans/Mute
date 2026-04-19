using System.Globalization;
using System.Threading.Tasks;
using Dapper;
using Mute.Moe.Services.Database;

namespace Mute.Moe.Services.Payment;

/// <summary>
/// Store transactions in the database
/// </summary>
public class DatabaseTransactions
    : ITransactions
{
    #region SQL transactions
    private const string InsertTransactionSql = "INSERT INTO `IOU2_Transactions` (`FromId`, `ToId`, `Amount`, `Unit`, `Note`, `InstantUnix`) VALUES (@FromId, @ToId, @Amount, @Unit, @Note, @InstantUnix); SELECT last_insert_rowid();";

    private const string GetFilteredTransactionsSql = "SELECT * FROM IOU2_Transactions WHERE (FromId = @FromId or @FromId IS null) AND (ToId = @ToId or @ToId IS null) AND (Unit = @Unit or @Unit IS null) AND (InstantUnix < @UpperBoundInstant or @UpperBoundInstant IS null) AND (InstantUnix > @LowerBoundInstant or @LowerBoundInstant IS NULL);";
    #endregion

    private readonly IDatabaseService _database;

    /// <summary>
    /// See new <see cref="DatabaseTransactions"/>
    /// </summary>
    /// <param name="database"></param>
    public DatabaseTransactions(IDatabaseService database)
    {
        _database = database;

        // Create debts table and indices
        using var connection = _database.GetConnection();
        connection.Execute("CREATE TABLE IF NOT EXISTS `IOU2_Transactions` (`ID` INTEGER PRIMARY KEY, `FromId` TEXT NOT NULL, `ToId` TEXT NOT NULL, `Amount` TEXT NOT NULL, `Unit` TEXT NOT NULL, `Note` TEXT, `InstantUnix` TEXT);");
    }

    /// <inheritdoc />
    public async Task CreateTransaction(ulong fromId, ulong toId, decimal amount, string unit, string? note, DateTime instant)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Cannot transact a negative amount");
        if (unit == null)
            throw new ArgumentNullException(nameof(unit));
        if (fromId == toId)
            throw new InvalidOperationException("Cannot transact from self to self");

        using var connection = _database.GetConnection();
        await connection.ExecuteScalarAsync<long>(
            InsertTransactionSql,
            new
            {
                FromId = fromId.ToString(),
                ToId = toId.ToString(),
                Amount = amount.ToString(CultureInfo.InvariantCulture),
                Unit = unit.ToLowerInvariant(),
                Note = note ?? "",
                InstantUnix = instant.UnixTimestamp().ToString()
            }
        );
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Transaction>> GetTransactions(ulong? fromId = null, ulong? toId = null, string? unit = null, DateTime? after = null, DateTime? before = null)
    {
        using var connection = _database.GetConnection();
        var rows = connection.QueryAsync<TransactionRow>(
            GetFilteredTransactionsSql,
            new
            {
                FromId = fromId?.ToString(),
                ToId = toId?.ToString(),
                Unit = unit?.ToLowerInvariant(),
                UpperBoundInstant = before?.UnixTimestamp().ToString(),
                LowerBoundInstant = after?.UnixTimestamp().ToString(),
            }
        );

        return await rows
              .ToAsyncEnumerable()
              .Select(a => a.ToTransaction())
              .ToArrayAsync();
    }

    private sealed record TransactionRow(long ID, string FromId, string ToId, string Amount, string Unit, string Note, string InstantUnix)
    {
        public Transaction ToTransaction()
        {
            return new Transaction(
                ulong.Parse(FromId),
                ulong.Parse(ToId),
                decimal.Parse(Amount, NumberStyles.Number, CultureInfo.InvariantCulture),
                Unit,
                Note,
                ulong.Parse(InstantUnix).FromUnixTimestamp()
            );
        }
    }
}