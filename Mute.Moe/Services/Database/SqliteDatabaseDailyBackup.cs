using Mute.Moe.Services.Host;
using System.Data.SQLite;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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
public partial class SqliteDatabaseDailyBackup
    : BaseDailyHostedService<SqliteDatabaseDailyBackup>, IDatabaseBackupService
{
    private readonly ILogger<SqliteDatabaseDailyBackup> _logger;

    private readonly IDatabaseService _database;
    private readonly string? _connectionString;

    private readonly AsyncLock _lock = new();

    /// <inheritdoc />
    public SqliteDatabaseDailyBackup(Configuration config, IDatabaseService database, ILogger<SqliteDatabaseDailyBackup> logger)
        : base(nameof(SqliteDatabaseDailyBackup), new TimeOnly(6, 5, 4), logger)
    {
        _database = database;
        _logger = logger;
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
                _logger.LogInformation("Cancelling backup - no connection string provided");
                return;
            }

            // Check source is compatible
            if (_database is not BaseSqliteDatabase sqlite)
            {
                _logger.LogWarning("Cannot backup non SQLite database");
                return;
            }

            var now = DateTime.UtcNow;
            var connStr = _connectionString.Replace("{{day_of_month}}", now.Day.ToString())
                                           .Replace("{{day_of_year}}", now.DayOfYear.ToString());

            // Open backup file
            LogOpeningBackupDb(connStr);
            await using var backup = new SQLiteConnection(connStr);
            backup.Open();

            // Do backup
            _logger.LogInformation("Beginning backup");
            await sqlite.Backup(backup);
            _logger.LogInformation("Completed backup");
        }
    }

    #region logging
    [LoggerMessage(LogLevel.Information, "Opening backup DB: {connStr}")]
    private partial void LogOpeningBackupDb(string connStr);
    #endregion
}