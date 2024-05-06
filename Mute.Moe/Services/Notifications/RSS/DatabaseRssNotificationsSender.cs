using System.Data.Common;
using System.Data.SQLite;
using System.ServiceModel.Syndication;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Discord;
using Discord.WebSocket;

using Mute.Moe.Extensions;
using Mute.Moe.Services.Database;
using Mute.Moe.Services.Information.RSS;

namespace Mute.Moe.Services.Notifications.RSS;

public class DatabaseRssNotificationsSender
    : IRssNotificationsSender
{
    private const string InsertNotificationSql = "INSERT into RssNotificationsSent (ChannelId, FeedUrl, UniqueId) values(@ChannelId, @FeedUrl, @UniqueId)";
    private const string HasPublishedNotification = "SELECT * FROM RssNotificationsSent Where (ChannelId = @ChannelId) AND (FeedUrl = @FeedUrl) AND (UniqueId = @UniqueId)";

    private readonly IDatabaseService _database;
    private readonly DiscordSocketClient _client;
    private readonly IRssNotifications _notifications;
    private readonly IRss _rss;

    private CancellationTokenSource? _cts;

    private static readonly TimeSpan PollDelay = TimeSpan.FromHours(1);

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
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = new CancellationTokenSource();
        _ = Task.Run(() => ThreadEntry(_cts.Token), _cts.Token);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_cts != null)
            await _cts.CancelAsync();
    }

    private async Task ThreadEntry(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                foreach (var feed in await _notifications.GetSubscriptions().ToListAsync(token))
                {
                    try
                    {
                        var syndication = await _rss.Fetch(feed.FeedUrl);

                        foreach (var item in syndication)
                        {
                            if (!await HasBeenPublished(feed.Channel.ToString(), feed.FeedUrl, item.Id))
                            {
                                await Publish(feed, item);
                                await Task.Delay(150, token);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"{nameof(DatabaseRssNotificationsSender)} Swallowed exception:\n{e}");
                    }
                }

                await Task.Delay(PollDelay, token);
            }
        }
        catch (Exception e)
        {
            var info = await _client.GetApplicationInfoAsync();
            if (info.Owner != null)
            {
                var channel = await info.Owner.CreateDMChannelAsync();
                await channel.SendMessageAsync($"{nameof(DatabaseRssNotificationsSender)} notifications thread crashed:");
                await channel.SendLongMessageAsync(e.ToString());
            }
        }
    }

    private async Task<bool> HasBeenPublished(string channelId, string feedUrl, string uniqueId)
    {
        return await new SqlAsyncResult<int>(_database, PrepareQuery, ParseSubscription).AnyAsync();

        DbCommand PrepareQuery(IDatabaseService db)
        {
            var cmd = db.CreateCommand();
            cmd.CommandText = HasPublishedNotification;
            cmd.Parameters.Add(new SQLiteParameter("@ChannelId", System.Data.DbType.String) { Value = channelId });
            cmd.Parameters.Add(new SQLiteParameter("@FeedUrl", System.Data.DbType.String) { Value = feedUrl });
            cmd.Parameters.Add(new SQLiteParameter("@UniqueId", System.Data.DbType.String) { Value = uniqueId });
            return cmd;
        }

        static int ParseSubscription(DbDataReader reader)
        {
            return 0;
        }
    }

    private async Task Publish(IRssSubscription feed, SyndicationItem item)
    {
        await SendMessage(feed, item);

        await using var cmd = _database.CreateCommand();
        cmd.CommandText = InsertNotificationSql;
        cmd.Parameters.Add(new SQLiteParameter("@ChannelId", System.Data.DbType.String) { Value = feed.Channel.ToString() });
        cmd.Parameters.Add(new SQLiteParameter("@FeedUrl", System.Data.DbType.String) { Value = feed.FeedUrl });
        cmd.Parameters.Add(new SQLiteParameter("@UniqueId", System.Data.DbType.String) { Value = item.Id });

        await cmd.ExecuteNonQueryAsync();
    }

    private static EmbedBuilder FormatMessage(SyndicationItem item)
    {
        var desc = item.Summary?.Text ?? "";
        desc = desc[..Math.Min(desc.Length, 1000)];
        desc = System.Net.WebUtility.HtmlDecode(desc);

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

    private async Task SendMessage(IRssSubscription feed, SyndicationItem item)
    {
        if (_client.GetChannel(feed.Channel) is not ITextChannel channel)
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