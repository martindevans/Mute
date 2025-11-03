using System.Threading;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Notifications.Cron;

/// <summary>
/// Runs a job on a set interval
/// </summary>
public interface ICron
{
    /// <summary>
    /// Start a new cron job
    /// </summary>
    /// <param name="duration"></param>
    /// <param name="act"></param>
    /// <param name="iterations"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task Interval(TimeSpan duration, Func<Task> act, int iterations, CancellationToken ct = default);
}