﻿using System.Data.Common;
using System.Data.SQLite;
using System.Globalization;
using System.Threading.Tasks;

using Mute.Moe.Extensions;
using Mute.Moe.Services.Database;

namespace Mute.Moe.Services.Payment;

public class DatabaseTransactions
    : ITransactions
{
    #region SQL transactions
    private const string InsertTransactionSql = "INSERT INTO `IOU2_Transactions` (`FromId`, `ToId`, `Amount`, `Unit`, `Note`, `InstantUnix`) VALUES (@FromId, @ToId, @Amount, @Unit, @Note, @InstantUnix); SELECT last_insert_rowid();";

    private const string GetFilteredTransactionsSql = "SELECT * FROM IOU2_Transactions WHERE (FromId = @FromId or @FromId IS null) AND (ToId = @ToId or @ToId IS null) AND (Unit = @Unit or @Unit IS null) AND (InstantUnix < @UpperBoundInstant or @UpperBoundInstant IS null) AND (InstantUnix > @LowerBoundInstant or @LowerBoundInstant IS NULL);";
    #endregion

    private readonly IDatabaseService _database;

    public DatabaseTransactions(IDatabaseService database)
    {
        _database = database;

        //Create debts table and indices
        _database.Exec("CREATE TABLE IF NOT EXISTS `IOU2_Transactions` (`FromId` TEXT NOT NULL, `ToId` TEXT NOT NULL, `Amount` TEXT NOT NULL, `Unit` TEXT NOT NULL, `Note` TEXT, `InstantUnix` TEXT);");
    }

    public async Task CreateTransaction(ulong fromId, ulong toId, decimal amount, string unit, string? note, DateTime instant)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Cannot transact a negative amount");
        if (unit == null)
            throw new ArgumentNullException(nameof(unit));
        if (fromId == toId)
            throw new InvalidOperationException("Cannot transact from self to self");

        await using var cmd = _database.CreateCommand();
        cmd.CommandText = InsertTransactionSql;
        cmd.Parameters.Add(new SQLiteParameter("@FromId", System.Data.DbType.String) { Value = fromId.ToString() });
        cmd.Parameters.Add(new SQLiteParameter("@ToId", System.Data.DbType.String) { Value = toId.ToString() });
        cmd.Parameters.Add(new SQLiteParameter("@Amount", System.Data.DbType.String) { Value = amount.ToString(CultureInfo.InvariantCulture) });
        cmd.Parameters.Add(new SQLiteParameter("@Unit", System.Data.DbType.String) { Value = unit.ToLowerInvariant() });
        cmd.Parameters.Add(new SQLiteParameter("@Note", System.Data.DbType.String) { Value = note ?? "" });
        cmd.Parameters.Add(new SQLiteParameter("@InstantUnix", System.Data.DbType.String) { Value = instant.UnixTimestamp() });

        // ReSharper disable once UnusedVariable
        var id = await cmd.ExecuteScalarAsync();
    }

    public IAsyncEnumerable<ITransaction> GetTransactions(ulong? fromId = null, ulong? toId = null, string? unit = null, DateTime? after = null, DateTime? before = null)
    {
        return new SqlAsyncResult<ITransaction>(_database, PrepareQuery, ParseTransaction);

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

        static ITransaction ParseTransaction(DbDataReader reader)
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