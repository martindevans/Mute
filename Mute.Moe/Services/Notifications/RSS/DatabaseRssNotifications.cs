using Mute.Moe.Services.Database;
using Serilog;
using System.Data.Common;
using System.Threading.Tasks;
using Dapper;

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
        return new SqlAsyncResult<IRssSubscription>(_database, PrepareQuery, ParseSubscription);

        static DbCommand PrepareQuery(IDatabaseService db)
        {
            var cmd = db.CreateCommand();
            cmd.CommandText = GetSubscriptionsSql;
            return cmd;
        }

        static IRssSubscription ParseSubscription(DbDataReader reader)
        {
            var mention = reader["MentionGroup"];
            return new RssSubscription(
                reader["Url"].ToString()!,
                ulong.Parse(reader["ChannelId"].ToString()!),
                mention is DBNull ? null : ulong.Parse(mention.ToString()!)
            );
        }
    }

    private record RssSubscription(string FeedUrl, ulong Channel, ulong? MentionRole) : IRssSubscription;
}