using Mute.Moe.Services.Host;
using Serilog;
using System.Threading;
using System.Threading.Tasks;
using static System.Data.Entity.Infrastructure.Design.Executor;
using ILogger = Serilog.ILogger;

namespace Mute.Moe.Services.LLM.Memory;

/// <summary>
/// Applies memory confidence decay once per day
/// </summary>
/// <param name="_config"></param>
/// <param name="_store"></param>
[UsedImplicitly]
public class AgentMemoryConfidenceDecayOverTime(Configuration _config, IAgentMemoryStorage _store)
    : IHostedService
{
    private static readonly ILogger _logger = Log.ForContext<AgentMemoryConfidenceDecayOverTime>();

    private readonly CancellationTokenSource _cancellation = new();
    private Task? _task;

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _task = Task.Run(() => DecayLoop(cancellationToken), cancellationToken);
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _cancellation.CancelAsync();
        if (_task != null)
            await _task;
    }

    private async Task DecayLoop(CancellationToken cancellation)
    {
        var config = _config.Agent.MemoryDecay;

        while (!cancellation.IsCancellationRequested)
        {
            await WaitForNextTime(
                config.Hour   ?? 11,
                config.Minute ?? 11,
                config.Second ?? 11,
                cancellation
            );

            _logger.Information("Beginning memory maintenance cycle");

            var deleted = await _store.DeleteMemoryWithoutEvidence();
            _logger.Information("Deleted {0} invalid/dangling memory items", deleted);

            var decayed = await ApplyDecay(
                config.Threshold,
                config.DecayValue
            );
            _logger.Information("Decayed {0} memories", decayed);

            _logger.Information("Completed memory maintenance cycle");
        }
    }

    private static async Task WaitForNextTime(int hour, int min, int sec, CancellationToken cancellation)
    {
        _logger.Information("Waiting for next decay time: {0}:{1}:{2}", hour, min, sec);

        var now = DateTime.Now;
        var next4AM = new DateTime(now.Year, now.Month, now.Day, hour, min, sec);
        if (now >= next4AM)
            next4AM = next4AM.AddDays(1);
        await Task.Delay(next4AM - now, cancellation);
    }

    /// <summary>
    /// Apply memory confidence decay to all memories below the given threshold
    /// </summary>
    /// <param name="threshold">Only effect memories with logit value below this value</param>
    /// <param name="amount">Subtract this from logits</param>
    /// <returns></returns>
    private async Task<int> ApplyDecay(float threshold, float amount)
    {
        if (amount < 0)
        {
            Log.Error("Cannot apply logit decay, amount ({0}) must be positive", amount);
            return 0;
        }

        return await _store.AddToConfidenceLogits(null, threshold, -amount);
    }
}