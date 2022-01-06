using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Introspection.Uptime
{
    public class UtcDifferenceUptime
        : IUptime
    {
        public DateTime StartTimeUtc { get; private set; }

        public TimeSpan Uptime => DateTime.UtcNow - StartTimeUtc;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            StartTimeUtc = DateTime.UtcNow;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
        }
    }
}
