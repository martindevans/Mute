using Mute.Moe.Services.Host;
using Serilog;
using System.Threading.Tasks;

namespace Mute.Moe.Services.LLM.Memory;

/// <summary>
/// Applies memory confidence decay once per day
/// </summary>
[UsedImplicitly]
public class AgentMemoryConfidenceDecayOverTime
    : BaseDailyHostedService<AgentMemoryConfidenceDecayOverTime>
{
    private static readonly ILogger _logger = Log.ForContext<AgentMemoryConfidenceDecayOverTime>();

    private readonly AgentConfig.MemoryDecayConfig _config;
    private readonly IAgentMemoryStorage _store;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="config"></param>
    /// <param name="store"></param>
    public AgentMemoryConfidenceDecayOverTime(Configuration config, IAgentMemoryStorage store)
        : base(
            nameof(AgentMemoryConfidenceDecayOverTime),
            new TimeOnly(config.Agent.MemoryDecay.Hour ?? 5, config.Agent.MemoryDecay.Minute ?? 6, config.Agent.MemoryDecay.Second ?? 7),
            TimeSpan.FromMinutes(1)
        )
    {
        _config = config.Agent.MemoryDecay;
        _store = store;
    }

    /// <inheritdoc />
    protected override async Task Execute()
    {
        var deleted = await _store.CleanupMemoryReferences();
        _logger.Information("Deleted {0} invalid/dangling memory items", deleted);

        var decayed = await ApplyDecay(
            _config.Threshold,
            _config.DecayValue
        );
        _logger.Information("Decayed {0} memories", decayed);
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