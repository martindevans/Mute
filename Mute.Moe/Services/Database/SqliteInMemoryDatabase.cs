using System.Data;
using System.Data.Common;
using System.Data.SQLite;

namespace Mute.Moe.Services.Database;

public class SqliteInMemoryDatabase
    : IDatabaseService
{
    private readonly SQLiteConnection _connection;

    public IDbConnection Connection => _connection;

    public SqliteInMemoryDatabase()
    {
        _connection = new SQLiteConnection("Data Source=:memory:");
        _connection.Open();
    }

    public DbCommand CreateCommand()
    {
        return _connection.CreateCommand();
    }
}