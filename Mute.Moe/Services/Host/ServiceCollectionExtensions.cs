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
    /// <typeparam name="TConcrete"></typeparam>
    /// <param name="collection"></param>
    public static void AddHostedService<TConcrete>(this IServiceCollection collection)
        where TConcrete : class, IHostedService
    {
        collection.AddSingleton<TConcrete>();

        collection.AddSingleton<IHostedService, TConcrete>(a => a.GetRequiredService<TConcrete>());
    }

    /// <summary>
    /// Add a <see cref="IHostedService"/> implementation as a singleton
    /// </summary>
    /// <typeparam name="TInterface"></typeparam>
    /// <typeparam name="TConcrete"></typeparam>
    /// <param name="collection"></param>
    public static void AddHostedService<TInterface, TConcrete>(this IServiceCollection collection)
        where TInterface : class
        where TConcrete : class, TInterface, IHostedService
    {
        collection.AddSingleton<TConcrete>();

        collection.AddSingleton<IHostedService, TConcrete>(a => a.GetRequiredService<TConcrete>());
        collection.AddSingleton<TInterface>(a => a.GetRequiredService<TConcrete>());
    }
}