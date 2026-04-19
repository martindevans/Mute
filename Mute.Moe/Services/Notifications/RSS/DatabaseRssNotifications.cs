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
            using var connection = _database.GetConnection();
            connection.Execute("CREATE TABLE IF NOT EXISTS `RssSubscriptions` (`Url` TEXT NOT NULL, `ChannelId` TEXT NOT NULL, `MentionGroup` TEXT)");
        }
        catch (Exception e)
        {
            Log.Error(e, "Creating 'RssSubscriptions' table failed");
        }
    }

    /// <inheritdoc />
    public async Task Subscribe(string feedUrl, ulong channel, ulong? mentionGroup)
    {
        using var connection = _database.GetConnection();
        await connection.ExecuteAsync(
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
        using var connection = _database.GetConnection();
        await connection.ExecuteAsync(
            DeleteSubscriptionSql,
            new
            {
                Url = feedUrl,
                ChannelId = channel.ToString(),
            }
        );
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IRssSubscription>> GetSubscriptions()
    {
        using var connection = _database.GetConnection();
        var rows = connection.QueryAsync<RssSubscriptionRow>(GetSubscriptionsSql);

        return await rows
            .ToAsyncEnumerable()
            .Select(a => a.ToSubscription())
            .ToArrayAsync();
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