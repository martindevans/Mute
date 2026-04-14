using Mute.Moe.Services.Host;
using Serilog;
using System.Data.SQLite;
using System.Threading;
using System.Threading.Tasks;
using Mute.Moe.Utilities;

namespace Mute.Moe.Services.Database;

/// <summary>
/// Backup the DB
/// </summary>
public interface IDatabaseBackupService
{
    /// <summary>
    /// Do a backup now
    /// </summary>
    public Task Backup(CancellationToken cancellation);
}

/// <summary>
/// Runs a backup of the main database each day
/// </summary>
[UsedImplicitly]
public class SqliteDatabaseDailyBackup
    : BaseDailyHostedService<SqliteDatabaseDailyBackup>, IDatabaseBackupService
{
    private static readonly ILogger _logger = Log.ForContext<SqliteDatabaseDailyBackup>();

    private readonly IDatabaseService _database;
    private readonly string? _connectionString;

    private readonly AsyncLock _lock = new();

    /// <inheritdoc />
    public SqliteDatabaseDailyBackup(Configuration config, IDatabaseService database)
        : base(nameof(SqliteDatabaseDailyBackup), new TimeOnly(6, 5, 4))
    {
        _database = database;
        _connectionString = config.Database?.BackupConnectionString;
    }

    /// <inheritdoc />
    protected override async Task Execute(CancellationToken cancellation)
    {
        await Backup(cancellation);
    }

    /// <inheritdoc />
    public async Task Backup(CancellationToken cancellation)
    {
        using (await _lock.LockAsync(cancellation))
        {
            // Check if we have a destination
            if (_connectionString == null)
            {
                _logger.Information("Cancelling backup - no connection string provided");
                return;
            }

            // Check source is compatible
            if (_database is not SqliteDatabase sqlite)
            {
                _logger.Warning("Cannot backup non SQLite database");
                return;
            }

            var now = DateTime.UtcNow;
            var connStr = _connectionString.Replace("{{day_of_month}}", now.Day.ToString())
                                           .Replace("{{day_of_year}}", now.DayOfYear.ToString());

            // Open backup file
            _logger.Information("Opening backup DB: {0}", connStr);
            await using var backup = new SQLiteConnection(connStr);
            backup.Open();

            // Do backup
            _logger.Information("Beginning backup");
            await sqlite.Backup(backup);
            _logger.Information("Completed backup");
        }
    }
}