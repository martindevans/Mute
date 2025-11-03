using System.Threading;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Host;

/// <summary>
/// A service which receives calls to start and stop, for long running/background tasks
/// </summary>
public interface IHostedService
{
    /// <summary>
    /// Start the service
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Stop the service
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task StopAsync(CancellationToken cancellationToken);
}