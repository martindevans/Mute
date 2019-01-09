using System;

namespace Mute.Services
{
    public class UptimeService
        : IPreloadService
    {
        public DateTime StartTimeUtc { get; } = DateTime.UtcNow;

        public TimeSpan Uptime => DateTime.UtcNow - StartTimeUtc;
    }
}
