using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Mute.Moe.Services.Introspection.Uptime;

/// <summary>
/// Gets uptime from helper in <see cref="Program"/>
/// </summary>
[ExcludeFromCodeCoverage]
public class ProcessUptime
    : IUptime
{
    private DateTime? _startTimeCache;
    
    /// <inheritdoc />
    public TimeSpan Uptime
    {
        get
        {
            if (!_startTimeCache.HasValue)
            {
                using var process = Process.GetCurrentProcess();
                _startTimeCache = process.StartTime;
            }

            return DateTime.UtcNow - _startTimeCache.Value;
        }
    }
}