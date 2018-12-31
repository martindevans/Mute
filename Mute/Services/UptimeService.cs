using System;

namespace Mute.Services
{
    public class UptimeService
    {
        public DateTime StartTimeUtc { get; } = DateTime.UtcNow;

        public TimeSpan Uptime => DateTime.UtcNow - StartTimeUtc;
    }
}
