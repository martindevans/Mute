namespace Mute.Moe.Tools.Providers;

/// <summary>
/// Provides date and time info
/// </summary>
public class ClockProvider
    : IToolProvider
{
    /// <inheritdoc />
    public IReadOnlyList<ITool> Tools { get; } =
    [
        new AutoTool("clock", true, GetClock)
    ];

    /// <summary>
    /// Get the current date and time
    /// </summary>
    /// <returns></returns>
    private static object GetClock()
    {
        var now = DateTime.UtcNow;
        var local = now.ToLocalTime();

        return new
        {
            utc = now.ToString("yyyy-MM-dd HH:mm:ss"),
            local = local.ToString("yyyy-MM-dd HH:mm:ss"),
            tz = TimeZoneInfo.Local.Id,
        };
    }
}