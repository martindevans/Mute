using System.Data.Common;
using System.Data.SQLite;
using System.Threading.Tasks;
using Mute.Moe.Services.Database;

namespace Mute.Moe.Services.Reminders;

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

    public event Action<IReminder>? ReminderCreated;
    public event Action<uint>? ReminderDeleted;

    public DatabaseReminders(IDatabaseService database)
    {
        _database = database;

        try
        {
            _database.Exec("CREATE TABLE IF NOT EXISTS `Reminders2` (`InstantUnix` TEXT NOT NULL, `ChannelId` TEXT NOT NULL, `Prelude` TEXT, `Message` TEXT NOT NULL, `Deleted` BOOLEAN NOT NULL, `UserId` TEXT NOT NULL)");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public async Task<IReminder> Create(DateTime triggerTime, string prelude, string msg, ulong channelId, ulong userId)
    {
        try
        {
            var reminder = new Reminder(0, triggerTime, prelude, msg, channelId, userId);
            await using (var cmd = _database.CreateCommand())
            {
                cmd.CommandText = InsertReminder;
                reminder.Write(cmd);
                reminder = reminder.WithId((uint)(long)(await cmd.ExecuteScalarAsync())!);
            }

            ReminderCreated?.Invoke(reminder);

            return reminder;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

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

    public IOrderedAsyncEnumerable<IReminder> Get(ulong? userId = null, DateTime? after = null, DateTime? before = null, ulong? channel = null, uint? count = null)
    {
        return new SqlAsyncResult<IReminder>(_database, PrepareQuery, Reminder.Parse).OrderBy(a => a.TriggerTime);

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

    public class Reminder
        : IReminder, IComparable<IReminder>, IEquatable<IReminder>
    {
        public uint ID { get; }
        public ulong UserId { get; }
        public ulong ChannelId { get; }

        public DateTime TriggerTime { get; }

        public string Message { get; }
        public string? Prelude { get; }

        public Reminder(uint id, DateTime triggerTime, string? prelude, string message, ulong channelId, ulong userId)
        {
            ID = id;
            UserId = userId;
            ChannelId = channelId;

            TriggerTime = triggerTime;

            Message = message;
            Prelude = prelude;
        }

        public static Reminder Parse(DbDataReader reader)
        {
            return new Reminder(
                uint.Parse(reader["rowid"].ToString()!),
                ulong.Parse((string)reader["InstantUnix"]).FromUnixTimestamp(),
                reader["Prelude"].ToString(),
                reader["Message"].ToString()!,
                ulong.Parse((string)reader["ChannelId"]),
                ulong.Parse((string)reader["UserId"])
            );
        }

        public void Write(DbCommand cmd)
        {
            cmd.CommandText = InsertReminder;
            cmd.Parameters.Add(new SQLiteParameter("@InstantUnix", System.Data.DbType.String) {Value = TriggerTime.UnixTimestamp()});
            cmd.Parameters.Add(new SQLiteParameter("@ChannelId", System.Data.DbType.String) {Value = ChannelId.ToString()});
            cmd.Parameters.Add(new SQLiteParameter("@Prelude", System.Data.DbType.String) {Value = Prelude});
            cmd.Parameters.Add(new SQLiteParameter("@Message", System.Data.DbType.String) {Value = Message});
            cmd.Parameters.Add(new SQLiteParameter("@UserId", System.Data.DbType.String) {Value = UserId.ToString()});
        }

        public int CompareTo(IReminder? other)
        {
            if (ReferenceEquals(this, other))
                return 0;
            if (other is null)
                return 1;

            // ReSharper disable once ImpureMethodCallOnReadonlyValueField
            return TriggerTime.CompareTo(other.TriggerTime);
        }

        #region equality
        public bool Equals(IReminder? other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return ID == other.ID;
        }

        public override bool Equals(object? obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((IReminder)obj);
        }

        public override int GetHashCode()
        {
            return (int)ID;
        }

        public static bool operator ==(Reminder? left, IReminder? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Reminder? left, IReminder? right)
        {
            return !Equals(left, right);
        }

        public static bool operator ==(IReminder? left, Reminder? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(IReminder? left, Reminder? right)
        {
            return !Equals(left, right);
        }
        #endregion

        public Reminder WithId(uint id)
        {
            return new Reminder(
                id,
                TriggerTime,
                Prelude,
                Message,
                ChannelId,
                UserId
            );
        }
    }
}