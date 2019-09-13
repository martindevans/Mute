using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Notifications.RSS
{
    public interface IRssNotifications
    {
        Task Subscribe(string feedUrl, ulong channel, ulong? mentionGroup);

        Task<IAsyncEnumerable<IRssSubscription>> GetSubscriptions();
    }

    public interface IRssSubscription
    {
        string FeedUrl { get; }

        ulong Channel { get; }

        ulong? MentionRole { get; }
    }
}
