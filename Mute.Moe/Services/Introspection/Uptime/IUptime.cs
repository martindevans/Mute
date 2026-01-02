namespace Mute.Moe.Services.Introspection.Uptime;

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