using Mute.Moe.Services.Host;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
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
        while (!cancellation.IsCancellationRequested)
        {
            await WaitForNextTime(
                _config?.Agent?.MemoryDecay.Hour   ?? 04,
                _config?.Agent?.MemoryDecay.Minute ?? 30,
                _config?.Agent?.MemoryDecay.Second ?? 56,
                cancellation
            );

            await ApplyDecay(
                _config?.Agent?.MemoryDecay.Threshold ?? 0,
                _config?.Agent?.MemoryDecay.Factor ?? 1.1f
            );
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
    /// <param name="threshold"></param>
    /// <param name="factor"></param>
    /// <returns></returns>
    public async Task ApplyDecay(float threshold, float factor)
    {
        _logger.Information("Beginning memory confidence decay");

        if (threshold > 0)
        {
            _logger.Error("Cannot use threshold > 0 for confidence decay");
            threshold = 0;
        }

        if (factor < 1)
        {
            _logger.Error("Cannot use factor < 1 for confidence decay");
            factor = 1;
        }

        var updated = await _store.UpdateConfidenceDecay(
            threshold,
            factor
        );

        _logger.Information("Decayed {0} memories", updated);
    }
}