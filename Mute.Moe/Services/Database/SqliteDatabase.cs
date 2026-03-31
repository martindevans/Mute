using Serilog;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;

namespace Mute.Moe.Services.Database;

/// <summary>
/// SQLite database
/// </summary>
public class SqliteDatabase
    : IDatabaseService
{
    private readonly SQLiteConnection _dbConnection;

    /// <inheritdoc />
    public IDbConnection Connection => _dbConnection;

    /// <summary>
    /// Create new DB
    /// </summary>
    public SqliteDatabase(string connection)
    {
        Log.Information("DB Connection String: {0}", connection);

        _dbConnection = new SQLiteConnection(connection);
        _dbConnection.Open();

        _dbConnection.EnableExtensions(true);
        _dbConnection.LoadExtension("vector");
        _dbConnection.EnableExtensions(false);
    }
}

/// <summary>
/// SQLite database
/// </summary>
public class SqliteConfigDatabase
    : SqliteDatabase
{
    /// <summary>
    /// Create new SQLite database connection
    /// </summary>
    /// <param name="config"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public SqliteConfigDatabase(Configuration config)
        : base(config.Database?.ConnectionString ?? throw new ArgumentNullException(nameof(config.Database.ConnectionString)))
    {
    }
}

/// <summary>
/// Provides an entirely in-memory SQLite database that loses all data once this process ends
/// </summary>
public class SqliteInMemoryDatabase
    : SqliteDatabase
{
    /// <summary>
    /// Create new in-memory DB
    /// </summary>
    public SqliteInMemoryDatabase()
        : base("Data Source=:memory:")
    {
    }
}