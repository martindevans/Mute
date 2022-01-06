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

        public static void AddHostedService<TInterface, TConcrete>(this IServiceCollection collection)
            where TInterface : class, IHostedService
            where TConcrete : class, TInterface
        {
            collection.AddSingleton<TConcrete>();

            collection.AddSingleton<IHostedService, TConcrete>(a => a.GetRequiredService<TConcrete>());
            collection.AddSingleton<TInterface>(a => a.GetRequiredService<TConcrete>());
        }
    }
}
