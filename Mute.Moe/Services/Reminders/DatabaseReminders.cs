using Mute.Moe.Services.Database;
using Serilog;
using System.Data.Common;
using System.Data.SQLite;
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
            _database.Exec("CREATE TABLE IF NOT EXISTS `Reminders2` (`InstantUnix` TEXT NOT NULL, `ChannelId` TEXT NOT NULL, `Prelude` TEXT, `Message` TEXT NOT NULL, `Deleted` BOOLEAN NOT NULL, `UserId` TEXT NOT NULL)");
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
            await using (var cmd = _database.CreateCommand())
            {
                cmd.CommandText = InsertReminder;
                WriteReminder(reminder, cmd);
                reminder = reminder with
                {
                    ID = (uint)(long)(await cmd.ExecuteScalarAsync())!
                };
            }

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
        bool deleted;
        await using (var cmd = _database.CreateCommand())
        {
            cmd.CommandText = DeleteReminder;
            cmd.Parameters.Add(new SQLiteParameter("@ID", System.Data.DbType.UInt32) { Value = reminderId });
            cmd.Parameters.Add(new SQLiteParameter("@UserId", System.Data.DbType.String) { Value = userId });

            deleted = await cmd.ExecuteNonQueryAsync() > 0;
        }

        if (deleted)
            ReminderDeleted?.Invoke(reminderId);

        return deleted;
    }

    /// <inheritdoc />
    public IOrderedAsyncEnumerable<Reminder> Get(ulong? userId = null, DateTime? after = null, DateTime? before = null, ulong? channel = null, uint? count = null)
    {
        return new SqlAsyncResult<Reminder>(_database, PrepareQuery, ParseReminder).OrderBy(a => a.TriggerTime);

        DbCommand PrepareQuery(IDatabaseService db)
        {
            var cmd = db.CreateCommand();
            cmd.CommandText = GetFilteredRemindersSql;
            cmd.Parameters.Add(new SQLiteParameter("@UserId", System.Data.DbType.String) { Value = userId?.ToString() });
            cmd.Parameters.Add(new SQLiteParameter("@UpperBoundInstant", System.Data.DbType.String) { Value = before?.UnixTimestamp().ToString() });
            cmd.Parameters.Add(new SQLiteParameter("@LowerBoundInstant", System.Data.DbType.String) { Value = after?.UnixTimestamp().ToString() });
            cmd.Parameters.Add(new SQLiteParameter("@ChannelId", System.Data.DbType.String) { Value = channel?.ToString() });
            cmd.Parameters.Add(new SQLiteParameter("@Limit", System.Data.DbType.UInt32) { Value = count ?? uint.MaxValue });
            return cmd;
        }
    }

    private static Reminder ParseReminder(DbDataReader reader)
    {
        return new Reminder(
            ID: uint.Parse(reader["rowid"].ToString()!),
            TriggerTime: ulong.Parse((string)reader["InstantUnix"]).FromUnixTimestamp(),
            Prelude: reader["Prelude"].ToString(),
            Message: reader["Message"].ToString()!,
            ChannelId: ulong.Parse((string)reader["ChannelId"]),
            UserId: ulong.Parse((string)reader["UserId"])
        );
    }

    private static void WriteReminder(Reminder reminder, DbCommand cmd)
    {
        cmd.CommandText = InsertReminder;
        cmd.Parameters.Add(new SQLiteParameter("@InstantUnix", System.Data.DbType.String) { Value = reminder.TriggerTime.UnixTimestamp() });
        cmd.Parameters.Add(new SQLiteParameter("@ChannelId", System.Data.DbType.String) { Value = reminder.ChannelId.ToString() });
        cmd.Parameters.Add(new SQLiteParameter("@Prelude", System.Data.DbType.String) { Value = reminder.Prelude });
        cmd.Parameters.Add(new SQLiteParameter("@Message", System.Data.DbType.String) { Value = reminder.Message });
        cmd.Parameters.Add(new SQLiteParameter("@UserId", System.Data.DbType.String) { Value = reminder.UserId.ToString() });
    }
}