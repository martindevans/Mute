using System.Threading;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Host;

/// <summary>
/// A service which receivescalls to start and stop, for long running/background tasks
/// </summary>
public interface IHostedService
{
    Task StartAsync(CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}