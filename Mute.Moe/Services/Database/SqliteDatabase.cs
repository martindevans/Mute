using System;
using System.Data.Common;
using System.Data.SQLite;


namespace Mute.Moe.Services.Database;

public class SqliteDatabase
    : IDatabaseService
{
    private readonly SQLiteConnection _dbConnection;

    public SqliteDatabase(Configuration config)
    {
        Console.WriteLine($"Connection String: `{config.Database?.ConnectionString}`");
        _dbConnection = new SQLiteConnection(config.Database?.ConnectionString ?? throw new ArgumentNullException(nameof(config.Database.ConnectionString)));
        _dbConnection.Open();
    }

    public DbCommand CreateCommand()
    {
        return new SQLiteCommand(_dbConnection);
    }
}

// ReSharper disable once InconsistentNaming
public static class IDatabaseServiceExtensions
{
    public static int Exec(this IDatabaseService db, string sql)
    {
        using var cmd = db.CreateCommand();
        cmd.CommandText = sql;
        return cmd.ExecuteNonQuery();
    }
}