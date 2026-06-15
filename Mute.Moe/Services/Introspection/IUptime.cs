using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Mute.Moe.Services.Introspection;

/// <summary>
/// Measures process uptime
/// </summary>
public interface IUptime
{
    /// <summary>
    /// Get total uptime of this process
    /// </summary>
    TimeSpan Uptime { get; }
}

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