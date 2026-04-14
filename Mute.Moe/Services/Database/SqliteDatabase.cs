using Serilog;
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;

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

    /// <summary>
    /// Run a backup of this database to another database
    /// </summary>
    /// <param name="dest"></param>
    /// <returns></returns>
    public async Task Backup(SQLiteConnection dest)
    {
        await Task.Run(() =>
        {
            _dbConnection.BackupDatabase(
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
    public async Task Backup(SqliteDatabase dest)
    {
        await Backup(dest._dbConnection);
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