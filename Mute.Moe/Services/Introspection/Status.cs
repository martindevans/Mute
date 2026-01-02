using Discord.WebSocket;

namespace Mute.Moe.Services.Introspection;

/// <summary>
/// Various bits of status info
/// </summary>
public class Status
{
    private readonly DiscordSocketClient _client;

    /// <summary>
    /// Discord API latency
    /// </summary>
    public TimeSpan Latency => TimeSpan.FromMilliseconds(_client.Latency);

    /// <summary>
    /// Process memory working set
    /// </summary>
    public long MemoryWorkingSet => Environment.WorkingSet;

    /// <summary>
    /// Process GC memory usage
    /// </summary>
    public long TotalGCMemory => GC.GetTotalMemory(false);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="client"></param>
    public Status(DiscordSocketClient client)
    {
        _client = client;
    }
}