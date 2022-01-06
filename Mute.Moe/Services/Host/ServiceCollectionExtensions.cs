using Microsoft.Extensions.DependencyInjection;

namespace Mute.Moe.Services.Host
{
    public static class ServiceCollectionExtensions
    {
        public static void AddHostedService<T>(this IServiceCollection collection)
            where T : class, IHostedService
        {
            collection.AddSingleton<IHostedService, T>();
        }
    }
}
