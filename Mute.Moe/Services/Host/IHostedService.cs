using System.Threading;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Host;

public interface IHostedService
{
    Task StartAsync(CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}