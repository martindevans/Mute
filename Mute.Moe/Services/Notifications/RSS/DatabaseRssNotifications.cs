using Dapper;
using Mute.Moe.Services.Database;
using Serilog;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Notifications.RSS;

/// <inheritdoc />
public class DatabaseRssNotifications
    : IRssNotifications
{
    private const string InsertSubscriptionSql = "INSERT into `RssSubscriptions` (Url, ChannelId, MentionGroup) values(@Url, @ChannelId, @MentionGroup)";
    private const string DeleteSubscriptionSql = "DELETE from `RssSubscriptions` WHERE Url = @Url AND ChannelId = @ChannelId";
    private const string GetSubscriptionsSql = "SELECT * FROM RssSubscriptions";

    private readonly IDatabaseService _database;

    /// <summary>
    /// Create a new <see cref="DatabaseRssNotifications"/>
    /// </summary>
    /// <param name="database"></param>
    public DatabaseRssNotifications(IDatabaseService database)
    {
        _database = database;

        try
        {
            _database.Exec("CREATE TABLE IF NOT EXISTS `RssSubscriptions` (`Url` TEXT NOT NULL, `ChannelId` TEXT NOT NULL, `MentionGroup` TEXT)");
        }
        catch (Exception e)
        {
            Log.Error(e, "Creating 'RssSubscriptions' table failed");
        }
    }

    /// <inheritdoc />
    public async Task Subscribe(string feedUrl, ulong channel, ulong? mentionGroup)
    {
        await _database.Connection.ExecuteAsync(
            InsertSubscriptionSql,
            new
            {
                Url = feedUrl,
                ChannelId = channel.ToString(),
                MentionGroup = mentionGroup?.ToString(),
            }
        );
    }

    /// <inheritdoc />
    public async Task Unsubscribe(string feedUrl, ulong channel)
    {
        await _database.Connection.ExecuteAsync(
            DeleteSubscriptionSql,
            new
            {
                Url = feedUrl,
                ChannelId = channel.ToString(),
            }
        );
    }

    /// <inheritdoc />
    public IAsyncEnumerable<IRssSubscription> GetSubscriptions()
    {
        var rows = _database.Connection.QueryAsync<RssSubscriptionRow>(GetSubscriptionsSql);

        return rows.ToAsyncEnumerable()
                   .SelectMany(a => a)
                   .Select(a => a.ToSubscription());
    }

    private record RssSubscription(string FeedUrl, ulong Channel, ulong? MentionRole) : IRssSubscription;

    private record RssSubscriptionRow(string Url, string ChannelId, string? MentionGroup)
    {
        public RssSubscription ToSubscription()
        {
            return new(
                Url,
                ulong.Parse(ChannelId),
                MentionGroup == null ? null : ulong.Parse(MentionGroup)
            );
        }
    }
}