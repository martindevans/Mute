using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using Serilog;


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
    /// Create new SQLite database connection
    /// </summary>
    /// <param name="config"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public SqliteDatabase(Configuration config)
    {
        Log.Information("DB Connection String: {0}", config.Database?.ConnectionString);

        _dbConnection = new SQLiteConnection(config.Database?.ConnectionString ?? throw new ArgumentNullException(nameof(config.Database.ConnectionString)));
        _dbConnection.Open();

        _dbConnection.EnableExtensions(true);
        _dbConnection.LoadExtension("vector");
        _dbConnection.EnableExtensions(false);
    }

    /// <inheritdoc />
    public DbCommand CreateCommand()
    {
        return new SQLiteCommand(_dbConnection);
    }
}