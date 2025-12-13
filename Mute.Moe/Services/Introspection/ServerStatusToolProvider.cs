using Humanizer;
using Mute.Moe.Services.Introspection.Uptime;
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

    /// <inheritdoc />
    public IReadOnlyList<ITool> Tools { get; }

    /// <summary>
    /// Construct <see cref="ServerStatusToolProvider"/>
    /// </summary>
    /// <param name="uptime"></param>
    /// <param name="status"></param>
    public ServerStatusToolProvider(IUptime uptime, Status status)
    {
        _uptime = uptime;
        _status = status;

        Tools =
        [
            new AutoTool("get_self_uptime", false, GetUptime),
            new AutoTool("get_self_latency", false, GetLatency),
            new AutoTool("get_self_memory", false, GetMemory),
        ];
    }

    /// <summary>
    /// Get the total amount of time that this process has been running for.
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
    /// Get the current response time (i.e. latency) of the Discord API.
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
    /// Get the current memory usage of this process, including working set (the total bytes of physical memory mapped) and
    /// total GC memory (the heap size in bytes excluding fragmentation)
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
}