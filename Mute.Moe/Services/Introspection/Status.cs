using Discord.WebSocket;

namespace Mute.Moe.Services.Introspection;

public class Status
{
    private readonly DiscordSocketClient _client;

    public TimeSpan Latency => TimeSpan.FromMilliseconds(_client.Latency);

    public long MemoryWorkingSet => Environment.WorkingSet;
    public long TotalGCMemory => GC.GetTotalMemory(false);

    public Status(DiscordSocketClient client)
    {
        _client = client;
    }
}