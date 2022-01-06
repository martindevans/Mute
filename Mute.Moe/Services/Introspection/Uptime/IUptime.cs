using System;
using Mute.Moe.Services.Host;

namespace Mute.Moe.Services.Introspection.Uptime
{
    public interface IUptime
        : IHostedService
    {
        TimeSpan Uptime { get; }
    }
}
