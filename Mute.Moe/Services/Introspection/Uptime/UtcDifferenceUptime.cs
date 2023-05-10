using System;

namespace Mute.Moe.Services.Introspection.Uptime;

public class UtcDifferenceUptime
    : IUptime
{
    private DateTime CreationTimeUtc { get; }

    public TimeSpan Uptime => DateTime.UtcNow - CreationTimeUtc;

    public UtcDifferenceUptime()
    {
        CreationTimeUtc = DateTime.UtcNow;
    }
}