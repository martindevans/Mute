using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Threading.Tasks;

using Mute.Moe.Services.Database;

namespace Mute.Moe.Services.Notifications.RSS
{
    public class DatabaseRssNotifications
        : IRssNotifications
    {
        private const string InsertSubscriptionSql = "INSERT into RssSubscriptions (Url, ChannelId, MentionGroup) values(@Url, @ChannelId, @MentionGroup)";
        private const string GetSubscriptionsSql = "SELECT * FROM RssSubscriptions";

        private readonly IDatabaseService _database;

        public DatabaseRssNotifications(IDatabaseService database)
        {
            _database = database;

            try
            {
                _database.Exec("CREATE TABLE IF NOT EXISTS `RssSubscriptions` (`Url` TEXT NOT NULL, `ChannelId` TEXT NOT NULL, `MentionGroup` TEXT)");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public async Task Subscribe(string feedUrl, ulong channel, ulong? mentionGroup)
        {
            await using var cmd = _database.CreateCommand();
            cmd.CommandText = InsertSubscriptionSql;
            cmd.Parameters.Add(new SQLiteParameter("@Url", System.Data.DbType.String) { Value = feedUrl });
            cmd.Parameters.Add(new SQLiteParameter("@ChannelId", System.Data.DbType.String) { Value = channel.ToString() });
            cmd.Parameters.Add(new SQLiteParameter("@MentionGroup", System.Data.DbType.String) { Value = mentionGroup?.ToString() });

            await cmd.ExecuteNonQueryAsync();
        }

        public IAsyncEnumerable<IRssSubscription> GetSubscriptions()
        {
            static IRssSubscription ParseSubscription(DbDataReader reader)
            {
                var mention = reader["MentionGroup"];
                return new RssSubscription(
                    reader["Url"].ToString()!,
                    ulong.Parse(reader["ChannelId"].ToString()!),
                    mention is DBNull ? (ulong?)null : ulong.Parse(mention.ToString()!)
                );
            }

            static DbCommand PrepareQuery(IDatabaseService db)
            {
                var cmd = db.CreateCommand();
                cmd.CommandText = GetSubscriptionsSql;
                return cmd;
            }

            return new SqlAsyncResult<IRssSubscription>(_database, PrepareQuery, ParseSubscription);
        }

        private class RssSubscription
            : IRssSubscription
        {
            public string FeedUrl { get; }
            public ulong Channel { get; }
            public ulong? MentionRole { get; }

            public RssSubscription(string feedUrl, ulong channel, ulong? mentionRole)
            {
                Channel = channel;
                MentionRole = mentionRole;
                FeedUrl = feedUrl;
            }
        }
    }
}
