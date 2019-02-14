using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Mute.Moe.Services.Database;

namespace Mute.Moe.Services.Notifications.SpaceX
{
    public class DatabaseSpacexNotifications
        : ISpacexNotifications
    {
        private const string InsertSubscriptionSql = "INSERT into SpacexSubscriptions (ChannelId, MentionGroup) values(@ChannelId, @MentionGroup)";
        private const string GetSubscriptionsSql = "SELECT * FROM SpacexSubscriptions";

        private readonly IDatabaseService _database;

        public DatabaseSpacexNotifications(IDatabaseService database)
        {
            _database = database;

            try
            {
                _database.Exec("CREATE TABLE IF NOT EXISTS `SpacexSubscriptions` (`ChannelId` TEXT NOT NULL, `MentionGroup` TEXT)");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public async Task Subscribe(ulong channel, ulong? mentionGroup)
        {
            using (var cmd = _database.CreateCommand())
            {
                cmd.CommandText = InsertSubscriptionSql;
                cmd.Parameters.Add(new SQLiteParameter("@ChannelId", System.Data.DbType.String) { Value = channel.ToString() });
                cmd.Parameters.Add(new SQLiteParameter("@MentionGroup", System.Data.DbType.String) { Value = mentionGroup?.ToString() });

                await cmd.ExecuteNonQueryAsync();
            }
        }

        [NotNull, ItemNotNull] public Task<IAsyncEnumerable<ISpacexSubscription>> GetSubscriptions()
        {
            ISpacexSubscription ParseSubscription(DbDataReader reader)
            {
                var mention = reader["MentionGroup"];
                return new SpacexSubscription(
                    ulong.Parse(reader["ChannelId"].ToString()),
                    mention == null ? (ulong?)null : ulong.Parse(mention.ToString())
                );
            }

            DbCommand PrepareQuery(IDatabaseService db)
            {
                var cmd = db.CreateCommand();
                cmd.CommandText = GetSubscriptionsSql;
                return cmd;
            }

            var result = (IAsyncEnumerable<ISpacexSubscription>)new SqlAsyncResult<ISpacexSubscription>(_database, PrepareQuery, ParseSubscription);
            return Task.FromResult(result);
        }

        private class SpacexSubscription
            : ISpacexSubscription
        {
            public ulong Channel { get; }
            public ulong? MentionRole { get; }

            public SpacexSubscription(ulong channel, ulong? mentionRole)
            {
                Channel = channel;
                MentionRole = mentionRole;
            }
        }
    }
}
