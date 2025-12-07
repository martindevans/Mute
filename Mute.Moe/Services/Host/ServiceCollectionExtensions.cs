using Microsoft.Extensions.DependencyInjection;

namespace Mute.Moe.Services.Host;

/// <summary>
/// Extensions to the <see cref="IServiceCollection"/>
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add a <see cref="IHostedService"/> implementation as a singleton
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="collection"></param>
    public static void AddHostedService<T>(this IServiceCollection collection)
        where T : class, IHostedService
    {
        collection.AddSingleton<IHostedService, T>();
    }

    /// <summary>
    /// Add a <see cref="IHostedService"/> implementation as a singleton
    /// </summary>
    /// <typeparam name="TInterface"></typeparam>
    /// <typeparam name="TConcrete"></typeparam>
    /// <param name="collection"></param>
    public static void AddHostedService<TInterface, TConcrete>(this IServiceCollection collection)
        where TInterface : class, IHostedService
        where TConcrete : class, TInterface
    {
        collection.AddSingleton<TConcrete>();

        collection.AddSingleton<IHostedService, TConcrete>(a => a.GetRequiredService<TConcrete>());
        collection.AddSingleton<TInterface>(a => a.GetRequiredService<TConcrete>());
    }
}