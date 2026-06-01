using System.Diagnostics.CodeAnalysis;

namespace Mute.Moe.Services.Introspection.Uptime;

/// <summary>
/// Gets uptime from helper in <see cref="Program"/>
/// </summary>
[ExcludeFromCodeCoverage]
public class ProgramUptime
    : IUptime
{
    /// <inheritdoc />
    public TimeSpan Uptime => Program.Uptime();
}