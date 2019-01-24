using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using JetBrains.Annotations;
using Mute.Moe.Extensions;
using Mute.Moe.Services.Database;

namespace Mute.Moe.Discord.Services
{
    public class ReminderService
    {
        private const string InsertReminder = "INSERT into Reminders (UID, UtcTime, ChannelId, Prelude, Message, Sent, UserId) values(@Uid, @UtcTime, @ChannelId, @Prelude, @Message, \"False\", @UserId)";
        private const string UpdateSent = "UPDATE Reminders SET Sent = \"true\" Where UID = @Uid";
        private const string UnsentReminders = "SELECT * FROM Reminders where not Sent = \"true\"";
        private const string DeleteReminder = "DELETE FROM Reminders WHERE UID = @Uid";

        [NotNull] private readonly IDatabaseService _database;
        [NotNull] private readonly Random _random;
        [NotNull] private readonly DiscordSocketClient _client;

        private readonly AutoResetEvent _event = new AutoResetEvent(true);
        private Task _thread;

        private readonly List<Notification> _notifications = new List<Notification>();

        public ReminderService([NotNull] IDatabaseService database, [NotNull] Random random, [NotNull] DiscordSocketClient client)
        {
            _database = database;
            _random = random;
            _client = client;

            try
            {
                _database.Exec("CREATE TABLE IF NOT EXISTS `Reminders` (`UID` TEXT NOT NULL PRIMARY KEY, `UtcTime` TEXT NOT NULL, `ChannelId` TEXT NOT NULL, `Prelude` TEXT, `Message` TEXT NOT NULL, `Sent` NUMERIC NOT NULL, `UserId` TEXT NOT NULL)");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            _thread = Task.Run(ThreadEntry);

            Console.WriteLine("Started Reminder Service");
        }

        [NotNull] public IEnumerable<Notification> Get(ulong userId)
        {
            lock (_notifications)
                return _notifications.Where(n => n.UserId == userId).ToArray();
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
                            (string)reader["Prelude"],
                            (string)reader["Message"],
                            ulong.Parse((string)reader["ChannelId"]),
                            ulong.Parse((string)reader["UserId"])
                        );

                        lock (_notifications)
                            _notifications.Add(n);
                    }
                }
            }

            lock (_notifications)
                Console.WriteLine($"Loaded {_notifications.Count} reminders from database");
        }

        [ItemNotNull] public async Task<Notification> Create(DateTime utcTime, [CanBeNull] string prelude, string message, ulong channelId, ulong ownerId)
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
                    cmd.Parameters.Add(new SQLiteParameter("@Prelude", System.Data.DbType.String) { Value = prelude });
                    cmd.Parameters.Add(new SQLiteParameter("@Message", System.Data.DbType.String) { Value = message });
                    cmd.Parameters.Add(new SQLiteParameter("@UserId", System.Data.DbType.String) { Value = ownerId.ToString() });

                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            //Insert reminder into in memory cache
            var n = new Notification(id, utcTime, prelude, message, channelId, ownerId);
            lock (_notifications)
                _notifications.Add(n);

            //awake thread
            _event.Set();

            return n;
        }

        private async Task ThreadEntry()
        {
            try
            {
                await LoadFromDatabase();

                while (true)
                {
                    //Add in an unconditional sleep to keep things slow (we really don't need high resolution for reminders!)
                    await Task.Delay(1000);

                    //Check if there are any waiting events
                    Notification sendMe = null;
                    Notification next = null;
                    lock (_notifications)
                    {
                        if (_notifications.Count > 0)
                        {
                            //Sort by time
                            _notifications.Sort();

                            //Send the first notification if necessary
                            if (_notifications[0].TriggerTime <= DateTime.UtcNow)
                            {
                                sendMe = _notifications[0];
                                _notifications.RemoveAt(0);
                            }
                            else
                                next = _notifications[0];
                        }
                    }

                    if (sendMe != null)
                    {
                        //Send the notification if one was found
                        await SendNotification(sendMe);
                    }
                    else if (next != null)
                    {
                        //Wait until event should be sent or another event happens
                        var duration = (next.TriggerTime - DateTime.UtcNow);
                        var durationMillis = Math.Max(0, Math.Round(duration.TotalMilliseconds));
                        var intDurationMillis = durationMillis > int.MaxValue ? int.MaxValue : (int)durationMillis;
                        _event.WaitOne(intDurationMillis);
                    }
                    else
                    {
                        //No pending events, wait for something to happen
                        _event.WaitOne(10000);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Reminder service killed: " + e);
            }
        }

        private async Task SendNotification([NotNull] Notification notification)
        {
            Console.WriteLine($"Sending notification: C:{notification.ChannelId}, UID:{notification.ID}, Msg:{notification.Message}");

            //Send message
            var channel = (ISocketMessageChannel)_client.GetGuild(415655090842763265)?.GetChannel(notification.ChannelId);
            if (channel == null)
            {
                Console.WriteLine($"Cannot send reminder: Channel {notification.ChannelId} is null");
                return;
            }

            if (!string.IsNullOrWhiteSpace(notification.Prelude))
                await channel.SendMessageAsync(notification.Prelude);
            await channel.SendMessageAsync(notification.Message);

            //Mark as sent in the database
            try
            {
                using (var cmd = _database.CreateCommand())
                {
                    cmd.CommandText = UpdateSent;
                    cmd.Parameters.Add(new SQLiteParameter("@Uid", System.Data.DbType.String) { Value = notification.ID });

                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task<bool> Delete(string id)
        {
            //Delete from cache
            lock (_notifications)
            {
                var index = _notifications.FindIndex(n => n.ID == id);
                if (index == -1)
                    return false;
            }

            //if we found it, delete from database
            try
            {
                using (var cmd = _database.CreateCommand())
                {
                    cmd.CommandText = DeleteReminder;
                    cmd.Parameters.Add(new SQLiteParameter("@Uid", System.Data.DbType.String) { Value = id });

                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return true;
        }

        public class Notification
            : IComparable<Notification>
        {
            public readonly string ID;
            public readonly DateTime TriggerTime;
            public readonly string Prelude;
            public readonly string Message;
            public readonly ulong ChannelId;
            public readonly ulong UserId;

            public Notification(string id, DateTime triggerTime, string prelude, string message, ulong channelId, ulong userId)
            {
                ID = id;
                TriggerTime = triggerTime;
                Prelude = prelude;
                Message = message;
                ChannelId = channelId;
                UserId = userId;
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
