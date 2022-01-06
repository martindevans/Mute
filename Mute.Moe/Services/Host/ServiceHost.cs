using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Mute.Moe.Services.Host
{
    public class ServiceHost
    {
        private readonly IServiceProvider _provider;

        private readonly List<IHostedService> _services = new List<IHostedService>();

        public ServiceHost(IServiceProvider provider)
        {
            _provider = provider;
        }

        public async Task StartAsync(CancellationToken cancel)
        {
            var services = _provider.GetServices<IHostedService>().ToImmutableList();

            foreach (var service in services)
            {
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
}
