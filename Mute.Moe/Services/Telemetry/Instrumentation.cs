using System.Diagnostics;

namespace Mute.Moe.Services.Telemetry;

/// <summary>
/// Provides <see cref="ActivitySource"/> for instrumentation
/// </summary>
public sealed class Instrumentation
    : IDisposable
{
    /// <summary>
    /// Name used in activity source
    /// </summary>
    public const string ActivitySourceName = "Mute.Moe";
    
    /// <summary>
    /// Version used in activity source
    /// </summary>
    public const string ActivitySourceVersion = "1.0.0";

    /// <summary>
    /// The activity source
    /// </summary>
    public ActivitySource ActivitySource { get; }

    /// <summary>
    /// Create a new <see cref="Instrumentation"/>
    /// </summary>
    public Instrumentation()
    {
        ActivitySource = new ActivitySource(ActivitySourceName, ActivitySourceVersion);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        ActivitySource.Dispose();
    }
}