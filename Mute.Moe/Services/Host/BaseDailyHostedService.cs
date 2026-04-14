using System.Diagnostics;
using Serilog;
using System.Threading;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Host;

/// <summary>
/// Runs a service once each day
/// </summary>
public abstract class BaseDailyHostedService<TSelf>
    : IHostedService
{
    private static readonly ILogger _logger = Log.ForContext<TSelf>();

    private CancellationTokenSource _cancellation = new();
    private Task? _task;
    private readonly string _name;

    /// <summary>
    /// Time when this service triggers
    /// </summary>
    public TimeOnly Time { get; }
    
    /// <summary>
    /// Amount of random jitter to apply to trigger time
    /// </summary>
    public TimeSpan Jitter { get; }

    /// <summary>
    /// Create a new hosted daily service
    /// </summary>
    /// <param name="name"></param>
    /// <param name="time"></param>
    /// <param name="jitter"></param>
    public BaseDailyHostedService(string name, TimeOnly time, TimeSpan? jitter = null)
    {
        _name = name;
        Time = time;
        Jitter = jitter ?? TimeSpan.FromSeconds(1);
    }
    
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _task = Task.Run(() => ServiceLoop(_cancellation.Token), _cancellation.Token);
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _cancellation.CancelAsync();
            if (_task != null)
                await _task;
        }
        catch (TaskCanceledException)
        {
            // Swallow it.
        }
    }

    private async Task ServiceLoop(CancellationToken cancellation)
    {
        var rng = new Random();

        while (!cancellation.IsCancellationRequested)
        {
            // Wait for trigger time
            await WaitForNextTime(
                Time.Hour,
                Time.Minute,
                Time.Second,
                cancellation
            );

            // Random delay
            await Task.Delay(rng.NextSingle() * Jitter, cancellation);

            // Exit before going any further
            if (cancellation.IsCancellationRequested)
                break;

            _logger.Information("Beginning daily task: '{0}'", _name);

            // Enter a loop, retrying the task if it fails
            var timer = new Stopwatch();
            timer.Start();
            var success = false;
            const int RETRIES_MAX = 8;
            var attemptIndex = 0;
            for (attemptIndex = 0; attemptIndex < RETRIES_MAX; attemptIndex++)
            {
                _logger.Information("Attempting daily task: '{0}' {1}/{2}", _name, attemptIndex + 1, RETRIES_MAX);

                try
                {
                    // Do work
                    await Execute(cancellation);

                    // Exit if it succeeds
                    success = true;
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Daily task {0} failed", _name);
                }

                // Early out for cancellation
                if (cancellation.IsCancellationRequested)
                    break;

                // Wait longer and longer after more failures
                await Task.Delay(TimeSpan.FromMinutes(attemptIndex), cancellation);
            }
            timer.Stop();
            
            if (success)
                _logger.Information("Completed daily task: '{0}' with {1} attempts in {2}", _name, attemptIndex + 1, timer.Elapsed);
            else
                _logger.Information("Failed to complete daily task: '{0}' after {1} attempts in {2}", _name, RETRIES_MAX, timer.Elapsed);
        }
    }

    /// <summary>
    /// Do the daily task
    /// </summary>
    protected abstract Task Execute(CancellationToken cancellation);

    private async Task WaitForNextTime(int hour, int min, int sec, CancellationToken cancellation)
    {
        _logger.Information("Daily task '{0}' waiting for time: {1}:{2}:{3}", _name, hour, min, sec);

        var now = DateTime.Now;
        var next4AM = new DateTime(now.Year, now.Month, now.Day, hour, min, sec);
        if (now >= next4AM)
            next4AM = next4AM.AddDays(1);
        await Task.Delay(next4AM - now, cancellation);
    }
}