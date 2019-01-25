using System;

namespace Mute.Moe.Services.Introspection.Uptime
{
    public class UtcDifferenceUptime
        : IUptime
    {
        public DateTime StartTimeUtc { get; } = DateTime.UtcNow;

        public TimeSpan Uptime => DateTime.UtcNow - StartTimeUtc;
    }
}
