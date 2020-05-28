using System;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Reactive;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;
using Discord;
using Discord.WebSocket;

using Mute.Moe.Extensions;
using Mute.Moe.Services.Database;
using Mute.Moe.Services.Information.RSS;

namespace Mute.Moe.Services.Notifications.RSS
{
    public class DatabaseRssNotificationsSender
        : IRssNotificationsSender
    {
        private const string InsertNotificationSql = "INSERT into RssNotificationsSent (ChannelId, FeedUrl, UniqueId) values(@ChannelId, @FeedUrl, @UniqueId)";
        private const string HasPublishedNotification = "SELECT * FROM RssNotificationsSent Where (ChannelId = @ChannelId) AND (FeedUrl = @FeedUrl) AND (UniqueId = @UniqueId)";

        private readonly IDatabaseService _database;
        private readonly DiscordSocketClient _client;
        private readonly IRssNotifications _notifications;
        private readonly IRss _rss;

        private readonly Task _thread;
        
        public DatabaseRssNotificationsSender(DiscordSocketClient client, IRssNotifications notifications, IRss rss, IDatabaseService database)
        {
            _database = database;
            _client = client;
            _notifications = notifications;
            _rss = rss;

            try
            {
                _database.Exec("CREATE TABLE IF NOT EXISTS `RssNotificationsSent` (`ChannelId` TEXT NOT NULL, `FeedUrl` TEXT NOT NULL, `UniqueId` TEXT)");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            _thread = Task.Run(ThreadEntry);
        }

        private async Task ThreadEntry()
        {
            try
            {
                while (true)
                {
                    foreach (var feed in await _notifications.GetSubscriptions().ToArrayAsync())
                    {
                        try
                        {
                            var syndication = await _rss.Fetch(feed.FeedUrl);

                            foreach (var item in syndication)
                            {
                                if (!await HasBeenPublished(feed.Channel.ToString(), feed.FeedUrl, item.Id))
                                {
                                    await Publish(feed, item);
                                    await Task.Delay(100);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"{nameof(DatabaseRssNotificationsSender)} Swallowed exception:\n{e}");
                        }
                    }

                    await Task.Delay(TimeSpan.FromMinutes(5));
                }
            }
            catch (Exception e)
            {
                var info = await _client.GetApplicationInfoAsync();
                if (info.Owner != null)
                {
                    var channel = await info.Owner.GetOrCreateDMChannelAsync();
                    await channel.SendMessageAsync($"{nameof(DatabaseRssNotificationsSender)} notifications thread crashed:");
                    await channel.SendLongMessageAsync(e.ToString());
                }
            }
        }

        private async Task<bool> HasBeenPublished(string channelId, string feedUrl, string uniqueId)
        {
            static Unit ParseSubscription(DbDataReader reader)
            {
                return Unit.Default;
            }

            DbCommand PrepareQuery(IDatabaseService db)
            {
                var cmd = db.CreateCommand();
                cmd.CommandText = HasPublishedNotification;
                cmd.Parameters.Add(new SQLiteParameter("@ChannelId", System.Data.DbType.String) { Value = channelId });
                cmd.Parameters.Add(new SQLiteParameter("@FeedUrl", System.Data.DbType.String) { Value = feedUrl });
                cmd.Parameters.Add(new SQLiteParameter("@UniqueId", System.Data.DbType.String) { Value = uniqueId });
                return cmd;
            }

            return await new SqlAsyncResult<Unit>(_database, PrepareQuery, ParseSubscription).AnyAsync();
        }

        private async Task Publish( IRssSubscription feed,  SyndicationItem item)
        {
            await SendMessage(feed, item);

            await using var cmd = _database.CreateCommand();
            cmd.CommandText = InsertNotificationSql;
            cmd.Parameters.Add(new SQLiteParameter("@ChannelId", System.Data.DbType.String) { Value = feed.Channel.ToString() });
            cmd.Parameters.Add(new SQLiteParameter("@FeedUrl", System.Data.DbType.String) { Value = feed.FeedUrl });
            cmd.Parameters.Add(new SQLiteParameter("@UniqueId", System.Data.DbType.String) { Value = item.Id });

            await cmd.ExecuteNonQueryAsync();
        }

        private static EmbedBuilder FormatMessage( SyndicationItem item)
        {
            var desc = item.Summary?.Text ?? "";
            desc = desc.Substring(0, Math.Min(desc.Length, 1000));

            var embed = new EmbedBuilder()
                        .WithTitle(item.Title.Text)
                        .WithDescription(desc);

            // Try to get the date, if this is malformed the property throws in which case we'll just not include a date
            try
            {
                embed.WithTimestamp(item.PublishDate);
            }
            catch (XmlException)
            {
            }

            if (item.Links.Count > 0)
                embed = embed.WithUrl(item.Links[0].Uri.ToString());

            return embed;
        }

        private async Task SendMessage( IRssSubscription feed,  SyndicationItem item)
        {
            if (!(_client.GetChannel(feed.Channel) is ITextChannel channel))
                return;

            var mention = "";
            if (feed.MentionRole.HasValue && channel is IGuildChannel gc)
            {
                var role = gc.Guild.GetRole(feed.MentionRole.Value);
                if (role != null)
                    mention = $"{role.Mention}";
            }

            await channel.SendMessageAsync(mention, embed: FormatMessage(item).Build());
        }
    }
}
