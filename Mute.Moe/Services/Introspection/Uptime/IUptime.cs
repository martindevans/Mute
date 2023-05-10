using System;

namespace Mute.Moe.Services.Introspection.Uptime;

public interface IUptime
{
    TimeSpan Uptime { get; }
}