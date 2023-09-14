namespace Mute.Moe.Services.Introspection.Uptime;

public class UtcDifferenceUptime
    : IUptime
{
    private DateTime CreationTimeUtc { get; } = DateTime.UtcNow;

    public TimeSpan Uptime => DateTime.UtcNow - CreationTimeUtc;
}