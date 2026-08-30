using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Notifications.Cron;

/// <summary>
/// Executes cron jobs which are stored in memory (non-durable)
/// </summary>
[UsedImplicitly]
public sealed class InMemoryCron
    : ICron
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    private readonly ILogger<InMemoryCron> _logger;

    /// <summary>
    /// Create new <see cref="InMemoryCron"/>
    /// </summary>
    /// <param name="logger"></param>
    public InMemoryCron(ILogger<InMemoryCron> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task Interval(TimeSpan duration, Func<Task> act, int iterations = 1, CancellationToken ct = default)
    {
        return Task.Run(async () =>
        {
            for (var i = 0; i < iterations && !ct.IsCancellationRequested; i++)
            {
                await Task.Delay(duration, ct);
                await ExecuteAsync(act, ct);
            }
        }, ct);
    }

    /// <inheritdoc />
    public Task RandomInterval(TimeSpan min, TimeSpan max, Func<Task> act, CancellationToken ct = default)
    {
        var rng = new Random(act.GetHashCode());

        return Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                // Random delay
                var duration = min + (max - min) * rng.NextSingle();
                await Task.Delay(duration, ct);

                // Work
                await ExecuteAsync(act, ct);
            }
        }, ct);
    }

    /// <summary>
    /// Execute a job, logging and retrying once after a short delay if it fails
    /// </summary>
    /// <param name="act"></param>
    /// <param name="ct"></param>
    private async Task ExecuteAsync(Func<Task> act, CancellationToken ct)
    {
        try
        {
            await act();
        }
        catch (OperationCanceledException)
        {
            // Rethrow cancellation to terminate outer loop
            throw;
        }
        catch (Exception outerEx)
        {
            _logger.LogError(outerEx, "Cron job failed, retrying in {delay}", RetryDelay);

            await Task.Delay(RetryDelay, ct);

            try
            {
                await act();
            }
            catch (OperationCanceledException)
            {
                // Rethrow cancellation to terminate outer loop
                throw;
            }
            catch (Exception innerEx)
            {
                _logger.LogError(innerEx, "Cron job retry failed, resuming normal loop");
            }
        }
    }
}
