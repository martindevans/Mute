using System;

namespace Mute.Moe.Discord.Services
{
    public class UptimeService
    {
        public DateTime StartTimeUtc { get; } = DateTime.UtcNow;

        public TimeSpan Uptime => DateTime.UtcNow - StartTimeUtc;
    }
}
