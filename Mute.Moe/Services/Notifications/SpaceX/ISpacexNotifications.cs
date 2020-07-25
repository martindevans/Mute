using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Notifications.SpaceX
{
    public interface ISpacexNotifications
    {
        Task Subscribe(ulong channel, ulong? mentionGroup);

        IAsyncEnumerable<ISpacexSubscription> GetSubscriptions();
    }

    public interface ISpacexSubscription
    {
        ulong Channel { get; }
        ulong? MentionRole { get; }
    }
}
