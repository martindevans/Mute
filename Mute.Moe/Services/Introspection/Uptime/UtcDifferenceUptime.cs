namespace Mute.Moe.Services.Introspection.Uptime;

/// <summary>
/// Measure uptime by difference in `DateTime`
/// </summary>
public class UtcDifferenceUptime
    : IUptime
{
    private DateTime CreationTimeUtc { get; } = DateTime.UtcNow;

    /// <inheritdoc />
    public TimeSpan Uptime => DateTime.UtcNow - CreationTimeUtc;
}