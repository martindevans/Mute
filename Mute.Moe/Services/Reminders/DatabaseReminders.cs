using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Mute.Moe.AsyncEnumerable.Extensions;
using Mute.Moe.Extensions;
using Mute.Moe.Services.Database;

namespace Mute.Moe.Services.Reminders
{
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

        [NotNull] private readonly IDatabaseService _database;

        public event Action<IReminder> ReminderCreated;
        public event Action<uint> ReminderDeleted;

        public DatabaseReminders([NotNull] IDatabaseService database)
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
                uint id;
                using (var cmd = _database.CreateCommand())
                {
                    cmd.CommandText = InsertReminder;
                    cmd.Parameters.Add(new SQLiteParameter("@InstantUnix", System.Data.DbType.String) {Value = triggerTime.UnixTimestamp()});
                    cmd.Parameters.Add(new SQLiteParameter("@ChannelId", System.Data.DbType.String) {Value = channelId.ToString()});
                    cmd.Parameters.Add(new SQLiteParameter("@Prelude", System.Data.DbType.String) {Value = prelude});
                    cmd.Parameters.Add(new SQLiteParameter("@Message", System.Data.DbType.String) {Value = msg});
                    cmd.Parameters.Add(new SQLiteParameter("@UserId", System.Data.DbType.String) {Value = userId.ToString()});

                    id = (uint)(long)await cmd.ExecuteScalarAsync();
                }

                var reminder = new Reminder(id, triggerTime, prelude, msg, channelId, userId);

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
            using (var cmd = _database.CreateCommand())
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



        public async Task<IOrderedAsyncEnumerable<IReminder>> Get(ulong? userId = null, DateTime? after = null, DateTime? before = null, ulong? channel = null, uint? count = null)
        {
            IReminder ParseReminder(DbDataReader reader)
            {
                return new Reminder(
                    uint.Parse(reader["rowid"].ToString()),
                    ulong.Parse((string)reader["InstantUnix"]).FromUnixTimestamp(),
                    reader["Prelude"]?.ToString(),
                    reader["Message"].ToString(),
                    ulong.Parse((string)reader["ChannelId"]),
                    ulong.Parse((string)reader["UserId"])
                );
            }

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

            return new SqlAsyncResult<IReminder>(_database, PrepareQuery, ParseReminder).AsOrderedEnumerable(a => a.TriggerTime, Comparer<DateTime>.Default);
        }

        public class Reminder
            : IReminder, IComparable<IReminder>, IEquatable<IReminder>
        {
            public uint ID { get; }
            public ulong UserId { get; }
            public ulong ChannelId { get; }

            public DateTime TriggerTime { get; }

            public string Message { get; }
            public string Prelude { get; }

            public Reminder(uint id, DateTime triggerTime, string prelude, string message, ulong channelId, ulong userId)
            {
                ID = id;
                UserId = userId;
                ChannelId = channelId;

                TriggerTime = triggerTime;

                Message = message;
                Prelude = prelude;
            }

            public int CompareTo(IReminder other)
            {
                if (ReferenceEquals(this, other))
                    return 0;
                if (ReferenceEquals(null, other))
                    return 1;

                // ReSharper disable once ImpureMethodCallOnReadonlyValueField
                return TriggerTime.CompareTo(other.TriggerTime);
            }

            #region equality
            public bool Equals([CanBeNull] IReminder other)
            {
                if (ReferenceEquals(null, other))
                    return false;
                if (ReferenceEquals(this, other))
                    return true;
                return ID == other.ID;
            }

            public override bool Equals([CanBeNull] object obj)
            {
                if (ReferenceEquals(null, obj))
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

            public static bool operator ==([CanBeNull] Reminder left, [CanBeNull] IReminder right)
            {
                return Equals(left, right);
            }

            public static bool operator !=([CanBeNull] Reminder left, [CanBeNull] IReminder right)
            {
                return !Equals(left, right);
            }

            public static bool operator ==([CanBeNull] IReminder left, [CanBeNull] Reminder right)
            {
                return Equals(left, right);
            }

            public static bool operator !=([CanBeNull] IReminder left, [CanBeNull] Reminder right)
            {
                return !Equals(left, right);
            }
            #endregion
        }
    }
}
