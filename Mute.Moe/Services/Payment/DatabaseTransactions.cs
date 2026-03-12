using System.Data.Common;
using System.Data.SQLite;
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
        _database.Exec("CREATE TABLE IF NOT EXISTS `IOU2_Transactions` (`FromId` TEXT NOT NULL, `ToId` TEXT NOT NULL, `Amount` TEXT NOT NULL, `Unit` TEXT NOT NULL, `Note` TEXT, `InstantUnix` TEXT);");
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

        await _database.Connection.ExecuteScalarAsync<long>(
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
    public IAsyncEnumerable<Transaction> GetTransactions(ulong? fromId = null, ulong? toId = null, string? unit = null, DateTime? after = null, DateTime? before = null)
    {
        return new SqlAsyncResult<Transaction>(_database, PrepareQuery, ParseTransaction);

        DbCommand PrepareQuery(IDatabaseService db)
        {
            var cmd = db.CreateCommand();
            cmd.CommandText = GetFilteredTransactionsSql;
            cmd.Parameters.Add(new SQLiteParameter("@FromId", System.Data.DbType.String) { Value = fromId?.ToString() });
            cmd.Parameters.Add(new SQLiteParameter("@ToId", System.Data.DbType.String) { Value = toId?.ToString() });
            cmd.Parameters.Add(new SQLiteParameter("@Unit", System.Data.DbType.String) { Value = unit?.ToLowerInvariant() });
            cmd.Parameters.Add(new SQLiteParameter("@UpperBoundInstant", System.Data.DbType.String) { Value = before?.UnixTimestamp().ToString() });
            cmd.Parameters.Add(new SQLiteParameter("@LowerBoundInstant", System.Data.DbType.String) { Value = after?.UnixTimestamp().ToString() });
            return cmd;
        }

        static Transaction ParseTransaction(DbDataReader reader)
        {
            return new Transaction(
                ulong.Parse((string)reader["FromId"]),
                ulong.Parse((string)reader["ToId"]),
                decimal.Parse(reader["Amount"].ToString()!),
                (string)reader["Unit"],
                (string)reader["Note"],
                ulong.Parse((string)reader["InstantUnix"]).FromUnixTimestamp()
            );
        }
    }
}