using System.Threading.Tasks;

namespace Mute.Moe.Services.Notifications.RSS;

/// <summary>
/// Subscribe channels to RSS feeds
/// </summary>
public interface IRssNotifications
{
    /// <summary>
    /// Subscribe a channel to a feed
    /// </summary>
    /// <param name="feedUrl"></param>
    /// <param name="channel"></param>
    /// <param name="mentionGroup"></param>
    /// <returns></returns>
    Task Subscribe(string feedUrl, ulong channel, ulong? mentionGroup);

    /// <summary>
    /// Unsubscribe a channel from a feed
    /// </summary>
    /// <param name="feedUrl"></param>
    /// <param name="channel"></param>
    /// <returns></returns>
    Task Unsubscribe(string feedUrl, ulong channel);

    /// <summary>
    /// Get all active subscriptions
    /// </summary>
    /// <returns></returns>
    IAsyncEnumerable<IRssSubscription> GetSubscriptions();
}

/// <summary>
/// A Channel+Feed subscription
/// </summary>
public interface IRssSubscription
{
    /// <summary>
    /// URL of the RSS feed
    /// </summary>
    string FeedUrl { get; }

    /// <summary>
    /// Channek ID
    /// </summary>
    ulong Channel { get; }

    /// <summary>
    /// ID of a role that should be mentioned
    /// </summary>
    ulong? MentionRole { get; }
}