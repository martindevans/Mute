using System.Threading;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Notifications.Cron;

/// <summary>
/// Executes cron jobs which are stored in memory (non-durable)
/// </summary>
public class InMemoryCron
    : ICron
{
    /// <inheritdoc />
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