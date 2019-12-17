using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Notifications.Cron
{
    public interface ICron
    {
        Task Interval(TimeSpan duration, Func<Task> act, int iterations, CancellationToken ct = default);
    }
}
