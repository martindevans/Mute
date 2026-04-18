using System.Globalization;
using Dapper;
using Mute.Moe.Services.Database;
using Serilog;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Reminders;

/// <inheritdoc />
public class DatabaseReminders
    : IReminders
{
    private const string InsertReminder = "INSERT into Reminders2 (InstantUnix, ChannelId, Prelude, Message, Deleted, UserId) values(@InstantUnix, @ChannelId, @Prelude, @Message, False, @UserId); SELECT last_insert_rowid();";
    private const string DeleteReminder = "UPDATE Reminders2 SET Deleted = True Where ROWID = @ID AND (UserId = @UserId or @UserId IS null)";

    private const string GetFilteredRemindersSql = "SELECT *, rowid FROM Reminders2 " +
                                                   "WHERE NOT Deleted " +
                                                   "AND (ChannelId = @ChannelId or @ChannelId IS null) " +
                                                   "AND (UserId = @UserId or @UserId IS null) " +
                                                   "AND (InstantUnix < @UpperBoundInstant or @UpperBoundInstant IS null) " +
                                                   "AND (InstantUnix > @LowerBoundInstant or @LowerBoundInstant IS NULL) " +
                                                   "ORDER BY InstantUnix " +
                                                   "LIMIT @Limit";

    private readonly IDatabaseService _database;

    /// <inheritdoc />
    public event Action<Reminder>? ReminderCreated;

    /// <inheritdoc />
    public event Action<uint>? ReminderDeleted;

    /// <summary>
    /// Create a new reminder service that stores reminders in the given database
    /// </summary>
    /// <param name="database"></param>
    public DatabaseReminders(IDatabaseService database)
    {
        _database = database;

        try
        {
            using var connection = _database.GetConnection();
            connection.Execute("CREATE TABLE IF NOT EXISTS `Reminders2` (`InstantUnix` TEXT NOT NULL, `ChannelId` TEXT NOT NULL, `Prelude` TEXT, `Message` TEXT NOT NULL, `Deleted` BOOLEAN NOT NULL, `UserId` TEXT NOT NULL)");
        }
        catch (Exception e)
        {
            Log.Error(e, "Creating 'Reminders2' table failed");
        }
    }

    /// <inheritdoc />
    public async Task<Reminder> Create(DateTime triggerTime, string prelude, string msg, ulong channelId, ulong userId)
    {
        try
        {
            var reminder = new Reminder(0, triggerTime, prelude, msg, channelId, userId);

            using var connection = _database.GetConnection();
            var id = (uint)await connection.ExecuteScalarAsync<long>(
                InsertReminder,
                new
                {
                    InstantUnix = reminder.TriggerTime.UnixTimestamp(),
                    ChannelId = reminder.ChannelId.ToString(),
                    Prelude = reminder.Prelude,
                    Message = reminder.Message,
                    UserId = reminder.UserId.ToString(),
                }
            );

            reminder = reminder with { ID = id };

            ReminderCreated?.Invoke(reminder);

            return reminder;
        }
        catch (Exception e)
        {
            Log.Error(e, "Creating reminder {0} {1} @ {2} (user:{3}) failed", prelude, msg, triggerTime, userId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> Delete(ulong userId, uint reminderId)
    {
        using var connection = _database.GetConnection();
        var deleted = await connection.ExecuteAsync(
            DeleteReminder,
            new
            {
                ID = reminderId,
                UserId = userId.ToString(),
            }
        ) > 0;

        if (deleted)
            ReminderDeleted?.Invoke(reminderId);

        return deleted;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Reminder>> Get(ulong? userId = null, DateTime? after = null, DateTime? before = null, ulong? channel = null, uint? count = null)
    {
        using var connection = _database.GetConnection();
        var rows = connection.QueryAsync<ReminderRow>(
            GetFilteredRemindersSql,
            new
            {
                UserId = userId?.ToString(CultureInfo.InvariantCulture),
                UpperBoundInstant = before?.UnixTimestamp().ToString(CultureInfo.InvariantCulture),
                LowerBoundInstant = after?.UnixTimestamp().ToString(CultureInfo.InvariantCulture),
                ChannelId = channel?.ToString(CultureInfo.InvariantCulture),
                Limit = count ?? uint.MaxValue
            }
        );

        return await rows
                    .ToAsyncEnumerable()
                    .SelectMany(a => a)
                    .Select(row => row.ToReminder())
                    .ToArrayAsync();
    }

    private record ReminderRow(string InstantUnix, string ChannelId, string Prelude, string Message, bool Deleted, string UserId, long RowId)
    {
        public Reminder ToReminder()
        {
            return new(
                ID: (uint)RowId,
                TriggerTime: ulong.Parse(InstantUnix).FromUnixTimestamp(),
                Prelude: Prelude,
                Message: Message,
                ChannelId: ulong.Parse(ChannelId),
                UserId: ulong.Parse(UserId)
            );
        }
    }
}