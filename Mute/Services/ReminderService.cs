using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using JetBrains.Annotations;
using Mute.Extensions;

namespace Mute.Services
{
    public class ReminderService
    {
        private const string InsertReminder = "INSERT into Reminders (UID, UtcTime, ChannelId, Message, Sent) values(@Uid, @UtcTime, @ChannelId, @Message, \"False\")";
        private const string UpdateSent = "UPDATE Reminders SET Sent = \"true\" Where UID = @Uid";
        private const string UnsentReminders = "SELECT * FROM Reminders where not Sent = \"true\"";

        [NotNull] private readonly DatabaseService _database;
        [NotNull] private readonly Random _random;
        [NotNull] private readonly DiscordSocketClient _client;

        private readonly AutoResetEvent _event = new AutoResetEvent(true);
        private Task _thread;

        private readonly List<Notification> _notifications = new List<Notification>();

        public ReminderService([NotNull] DatabaseService database, [NotNull] Random random, [NotNull] DiscordSocketClient client)
        {
            _database = database;
            _random = random;
            _client = client;

            _database.Exec("CREATE TABLE IF NOT EXISTS `Reminders` (`UID` TEXT NOT NULL PRIMARY KEY, `UtcTime` TEXT NOT NULL, `ChannelId` TEXT NOT NULL, `Message` TEXT NOT NULL, `Sent` NUMERIC NOT NULL)");

            _thread = Task.Run(ThreadEntry);
        }

        private async Task LoadFromDatabase()
        {
            using (var cmd = _database.CreateCommand())
            {
                cmd.CommandText = UnsentReminders;

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var n = new Notification(
                            (string)reader["UID"],
                            DateTime.FromFileTimeUtc(long.Parse((string)reader["UtcTime"])),
                            (string)reader["Message"],
                            ulong.Parse((string)reader["ChannelId"])
                        );

                        lock (_notifications)
                            _notifications.Add(n);
                    }
                }
            }
        }

        public async Task Create(DateTime utcTime, string message, ulong channelId)
        {
            var id = unchecked((uint)_random.Next()).MeaninglessString();

            //Insert reminder into database
            try
            {
                using (var cmd = _database.CreateCommand())
                {
                    cmd.CommandText = InsertReminder;
                    cmd.Parameters.Add(new SQLiteParameter("@Uid", System.Data.DbType.String) { Value = id });
                    cmd.Parameters.Add(new SQLiteParameter("@UtcTime", System.Data.DbType.String) { Value = utcTime.ToFileTimeUtc() });
                    cmd.Parameters.Add(new SQLiteParameter("@ChannelId", System.Data.DbType.String) { Value = channelId.ToString() });
                    cmd.Parameters.Add(new SQLiteParameter("@Message", System.Data.DbType.String) { Value = message });

                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            //Insert reminder into in memory cache
            lock (_notifications)
                _notifications.Add(new Notification(id, utcTime, message, channelId));

            //awake thread
            _event.Set();
        }

        private async Task ThreadEntry()
        {
            await LoadFromDatabase();

            while (true)
            {
                //Check if there are any waiting events
                DateTime? next = null;
                lock (_notifications)
                {
                    if (_notifications.Count > 0)
                    {
                        _notifications.Sort();
                        next = _notifications[0].TriggerTime;
                    }
                }

                if (!next.HasValue)
                {
                    //no pending events, wait for a while
                    _event.WaitOne(10000);
                }
                else if (next.Value <= DateTime.UtcNow)
                {
                    //Send event
                    Notification n;
                    lock (_notifications)
                    {
                        n = _notifications[0];
                        _notifications.RemoveAt(0);
                    }
                    await SendNotification(n);
                }
                else
                {
                    //Wait until event should be sent or another event happens
                    _event.WaitOne((int)(next.Value - DateTime.UtcNow).TotalMilliseconds);
                }
            }
        }

        private async Task SendNotification([NotNull] Notification notification)
        {
            //Send message
            var channel = (ISocketMessageChannel)_client.GetChannel(notification.ChannelId);
            await channel.SendMessageAsync(notification.Message);
            
            //Mark as sent in the database
            try
            {
                using (var cmd = _database.CreateCommand())
                {
                    cmd.CommandText = UpdateSent;
                    cmd.Parameters.Add(new SQLiteParameter("@Uid", System.Data.DbType.String) { Value = notification.UID });

                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private class Notification
            : IComparable<Notification>
        {
            public readonly string UID;
            public readonly DateTime TriggerTime;
            public readonly string Message;
            public readonly ulong ChannelId;

            public Notification(string uid, DateTime triggerTime, string message, ulong channelId)
            {
                UID = uid;
                TriggerTime = triggerTime;
                Message = message;
                ChannelId = channelId;
            }

            public int CompareTo(Notification other)
            {
                if (ReferenceEquals(this, other))
                    return 0;
                if (ReferenceEquals(null, other))
                    return 1;

                // ReSharper disable once ImpureMethodCallOnReadonlyValueField
                return TriggerTime.CompareTo(other.TriggerTime);
            }
        }
    }
}
