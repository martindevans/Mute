﻿using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Mute.Moe.Services.Host;

public class ServiceHost(IServiceProvider provider)
{
    private readonly List<IHostedService> _services = [ ];

    public async Task StartAsync(CancellationToken cancel)
    {
        var services = provider.GetServices<IHostedService>().ToImmutableList();

        Console.WriteLine("Starting services:");
        foreach (var service in services)
        {
            Console.WriteLine($" - {service.GetType().Name}");
            await service.StartAsync(cancel);
            _services.Add(service);
        }
    }

    public async Task StopAsync(CancellationToken cancel)
    {
        foreach (var service in _services)
            await service.StopAsync(cancel);
        _services.Clear();
    }
}