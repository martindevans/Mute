using Serilog;
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Database;

/// <summary>
/// SQLite database
/// </summary>
public abstract class BaseSqliteDatabase
    : IDatabaseService
{
    private readonly string _dbConnStr;

    /// <summary>
    /// Create new DB
    /// </summary>
    public BaseSqliteDatabase(string connection)
    {
        Log.Information("DB Connection String: {0}", connection);

        _dbConnStr = connection;
        
        // Get a connection and dispose it now, should surface any errors earlier doing this.
        GetSqliteConnection().Dispose();
    }

    /// <summary>
    /// Get an <see cref="SQLiteConnection"/>
    /// </summary>
    /// <returns></returns>
    public SQLiteConnection GetSqliteConnection()
    {
        var connection = new SQLiteConnection(_dbConnStr);
        connection.Open();

        connection.EnableExtensions(true);
        connection.LoadExtension("vector");
        connection.EnableExtensions(false);

        return connection;
    }

    /// <inheritdoc />
    public IDbConnection GetConnection()
    {
        return GetSqliteConnection();
    }

    /// <summary>
    /// Run a backup of this database to another database
    /// </summary>
    /// <param name="dest"></param>
    /// <returns></returns>
    public async Task Backup(SQLiteConnection dest)
    {
        await Task.Run(() =>
        {
            using var connection = GetSqliteConnection();

            connection.BackupDatabase(
                destination: dest,
                destinationName: "main",
                sourceName: "main",
                pages: 8,
                callback: Callback,
                retryMilliseconds: 512
            );

            bool Callback(SQLiteConnection source, string sourceName, SQLiteConnection destination, string destinationName, int pages, int remainingPages, int totalPages, bool retry)
            {
                //Log.Information("{0}/{1}", totalPages - remainingPages, totalPages);
                return true;
            }
        });
    }

    /// <summary>
    /// Run a backup of this database to another database
    /// </summary>
    /// <param name="dest"></param>
    /// <returns></returns>
    public async Task Backup(BaseSqliteDatabase dest)
    {
        await using var dst = dest.GetSqliteConnection();
        await Backup(dst);
    }
}

/// <summary>
/// SQLite database
/// </summary>
public class SqliteConfigDatabase
    : BaseSqliteDatabase
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
    : BaseSqliteDatabase
{
    // The in memory DB will be lost as soon as the last connection is closed. This is a connection that
    // exists to "root" the database, but should never be used for queries (hence the type).
    // ReSharper disable once NotAccessedField.Local
    private readonly object _root;

    /// <summary>
    /// Create new in-memory DB
    /// </summary>
    public SqliteInMemoryDatabase()
        : base($"Data Source=file:{RandomName()}?mode=memory&cache=shared")
    {
        // One connection must always be open, to keep the DB alive
        _root = GetConnection();
    }

    private static string RandomName() => Random.Shared.GetHexString(32);
}