using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Notifications.Cron;

public class InMemoryCron
    : ICron
{
    public Task Interval(TimeSpan duration, Func<Task> act, int iterations = 1, CancellationToken ct = default)
    {
        return Task.Run(async () =>
        {
            for (var i = 0; i < iterations; i++)
            {
                await Task.Delay(duration, ct);
                await act();
            }
        }, ct);
    }
}