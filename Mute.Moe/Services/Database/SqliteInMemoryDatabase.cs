using System.Data;
using System.Data.Common;
using System.Data.SQLite;

namespace Mute.Moe.Services.Database;

public class SqliteInMemoryDatabase
    : IDatabaseService
{
    private readonly SQLiteConnection _dbConnection;

    public IDbConnection Connection => _dbConnection;

    public SqliteInMemoryDatabase()
    {
        _dbConnection = new SQLiteConnection("Data Source=:memory:");
        _dbConnection.Open();

        _dbConnection.EnableExtensions(true);
        _dbConnection.LoadExtension("vector");
        _dbConnection.EnableExtensions(false);
    }

    public DbCommand CreateCommand()
    {
        return _dbConnection.CreateCommand();
    }
}