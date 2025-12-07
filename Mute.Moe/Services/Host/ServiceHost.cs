using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Mute.Moe.Services.Host;

/// <summary>
/// Start and stop everything that implements <see cref="IHostedService"/>
/// </summary>
/// <param name="provider"></param>
public class ServiceHost(IServiceProvider provider)
{
    private readonly List<IHostedService> _services = [ ];

    /// <summary>
    /// Start all hosted services
    /// </summary>
    /// <param name="cancel"></param>
    /// <returns></returns>
    public async Task StartAsync(CancellationToken cancel)
    {
        var services = provider.GetServices<IHostedService>().ToImmutableList();

        foreach (var service in services)
        {
            Log.Information("StartAsync: {0}", service.GetType().Name);
            await service.StartAsync(cancel);
            _services.Add(service);
        }
    }

    /// <summary>
    /// Stop all hosted services
    /// </summary>
    /// <param name="cancel"></param>
    public async Task StopAsync(CancellationToken cancel)
    {
        foreach (var service in _services)
        {
            Log.Information("StopAsync: {0}", service.GetType().Name);
            await service.StopAsync(cancel);
        }
        _services.Clear();
    }
}