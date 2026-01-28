using System.Threading.Tasks;
using Humanizer;
using Mute.Moe.Services.Introspection.Uptime;
using Mute.Moe.Services.LLM;
using Mute.Moe.Tools;
using Mute.Moe.Tools.Providers;

namespace Mute.Moe.Services.Introspection;

/// <summary>
/// Provide tools related to the status of this server
/// </summary>
public class ServerStatusToolProvider
    : IToolProvider
{
    private readonly IUptime _uptime;
    private readonly Status _status;
    private readonly MultiEndpointProvider<LLamaServerEndpoint> _endpoints;

    /// <inheritdoc />
    public IReadOnlyList<ITool> Tools { get; }

    /// <summary>
    /// Construct <see cref="ServerStatusToolProvider"/>
    /// </summary>
    /// <param name="uptime"></param>
    /// <param name="status"></param>
    /// <param name="endpoints"></param>
    public ServerStatusToolProvider(IUptime uptime, Status status, MultiEndpointProvider<LLamaServerEndpoint> endpoints)
    {
        _uptime = uptime;
        _status = status;
        _endpoints = endpoints;

        Tools =
        [
            new AutoTool("get_self_uptime", false, GetUptime),
            new AutoTool("get_self_latency", false, GetLatency),
            new AutoTool("get_self_memory", false, GetMemory),
            new AutoTool("get_llm_cluster_status", false, GetLlmClusterStatus),
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
    /// - Availability check<br />
    /// - Latency (milliseconds)<br />
    /// - Current usage
    /// </summary>
    /// <returns></returns>
    private async Task<object> GetLlmClusterStatus()
    {
        var results = new Dictionary<string, object>();

        foreach (var endpoint in await _endpoints.GetStatus())
        {
            var load = (endpoint.MaxSlots - endpoint.AvailableSlots) / (float)endpoint.MaxSlots;

            results.Add(endpoint.Endpoint.ID, new
            {
                id = endpoint.Endpoint.ID,

                available = endpoint.Healthy,

                latency_ms = endpoint.Healthy ? (double?)endpoint.Latency.TotalMilliseconds : null,
                system_load = endpoint.Healthy ? load.ToString("P1") : null,
            });
        }

        return results;
    }
}