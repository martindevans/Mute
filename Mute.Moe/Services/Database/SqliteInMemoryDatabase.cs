using System.Data;
using System.Data.Common;
using System.Data.SQLite;

namespace Mute.Moe.Services.Database;

/// <summary>
/// Provides an entirely in-memory SQLite database that loses all data once this process ends
/// </summary>
public class SqliteInMemoryDatabase
    : IDatabaseService
{
    private readonly SQLiteConnection _dbConnection;

    /// <inheritdoc />
    public IDbConnection Connection => _dbConnection;

    /// <summary>
    /// Create new in-memory DB
    /// </summary>
    public SqliteInMemoryDatabase()
    {
        _dbConnection = new SQLiteConnection("Data Source=:memory:");
        _dbConnection.Open();

        _dbConnection.EnableExtensions(true);
        _dbConnection.LoadExtension("vector");
        _dbConnection.EnableExtensions(false);
    }

    /// <inheritdoc />
    public DbCommand CreateCommand()
    {
        return _dbConnection.CreateCommand();
    }
}