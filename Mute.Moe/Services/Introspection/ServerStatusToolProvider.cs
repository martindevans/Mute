using System.Threading;
using HandyAgentFramework;
using Humanizer;
using MultiBackendServiceProvider;
using Mute.Moe.Services.LLM;
using Mute.Moe.Tools;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Introspection;

/// <summary>
/// Provide tools related to the status of this server
/// </summary>
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
public class ServerStatusToolProvider
    : IToolProvider
{
    private readonly IUptime _uptime;
    private readonly Status _status;
    private readonly MultiBackendServiceProvider<LLamaServerEndpoint> _endpoints;

    /// <inheritdoc />
    public IEnumerable<ToolDefinition> Tools { get; }

    /// <summary>
    /// Construct <see cref="ServerStatusToolProvider"/>
    /// </summary>
    /// <param name="uptime"></param>
    /// <param name="status"></param>
    /// <param name="endpoints"></param>
    public ServerStatusToolProvider(IUptime uptime, Status status, MultiBackendServiceProvider<LLamaServerEndpoint> endpoints)
    {
        _uptime = uptime;
        _status = status;
        _endpoints = endpoints;

        Tools =
        [
            new DocStringTool(ToolGroups.Info.SelfStatus, "get_uptime", GetUptime),
            new DocStringTool(ToolGroups.Info.SelfStatus, "get_latency", GetLatency),
            new DocStringTool(ToolGroups.Info.SelfStatus, "get_memory", GetMemory),
            new DocStringTool(ToolGroups.Info.SelfStatus, "get_llm_cluster_availability", GetLlmClusterStatus),
        ];
    }

    /// <summary>
    /// Get the total amount of time that this process (i.e. the bot/assistant) has been running for.
    /// </summary>
    /// <returns></returns>
    private object GetUptime()
    {
        var time = _uptime.Uptime;
        return new
        {
            TotalSeconds = time.TotalSeconds,
            TotalMinutes = time.TotalMinutes,
            TotalHours = time.TotalHours,
            TotalDays = time.TotalDays,
            Humanized = time.Humanize()
        };
    }

    /// <summary>
    /// Get the current response time (i.e. latency) of the Discord API to this process (i.e. the bot/assistant).
    /// </summary>
    /// <returns></returns>
    private object GetLatency()
    {
        return new
        {
            TotalMilliseconds = _status.Latency.TotalMilliseconds,
        };
    }

    /// <summary>
    /// Get the current memory usage of this process (i.e. the bot/assistant). This Includes working set (the total bytes of physical memory mapped) and
    /// total GC memory (the heap size in bytes, excluding fragmentation).
    /// </summary>
    /// <returns></returns>
    private object GetMemory()
    {
        return new
        {
            MemoryWorkingSet = _status.MemoryWorkingSet,
            TotalGCMemory = _status.TotalGCMemory
        };
    }

    /// <summary>
    /// Get the current status of the cluster of LLM (large language model) servers which serve as the backend for the bot/assistant.
    ///
    /// Includes:<br />
    /// - ID<br />
    /// - Availability<br />
    /// - Current load percentage
    /// </summary>
    /// <returns></returns>
    private async Task<Dictionary<string, LlmClusterNodeStatus>> GetLlmClusterStatus(CancellationToken cancellation)
    {
        var results = new Dictionary<string, LlmClusterNodeStatus>();
        
        foreach (var endpoint in _endpoints.Backends)
        {
            var load = (endpoint.TotalSlots - endpoint.AvailableSlots) / (float)endpoint.TotalSlots;
            var healthy = await endpoint.CheckHealth(cancellation);

            results.Add(endpoint.Value.ID, new(
                Available: healthy,
                Load: healthy ? load.ToString("P1") : null
            ));
        }

        return results;
    }

    [UsedImplicitly]
    private record LlmClusterNodeStatus(bool Available, string? Load);
}