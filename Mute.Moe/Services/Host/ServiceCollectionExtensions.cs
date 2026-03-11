using Microsoft.Extensions.DependencyInjection;

namespace Mute.Moe.Services.Host;

/// <summary>
/// Extensions to the <see cref="IServiceCollection"/>
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <param name="this"></param>
    extension(IServiceCollection @this)
    {
        /// <summary>
        /// Add a <see cref="IHostedService"/> implementation as a singleton
        /// </summary>
        /// <typeparam name="TConcrete"></typeparam>
        public void AddHostedService<TConcrete>()
            where TConcrete : class, IHostedService
        {
            @this.AddSingleton<TConcrete>();

            @this.AddSingleton<IHostedService, TConcrete>(a => a.GetRequiredService<TConcrete>());
        }

        /// <summary>
        /// Add a <see cref="IHostedService"/> implementation as a singleton
        /// </summary>
        /// <typeparam name="TInterface"></typeparam>
        /// <typeparam name="TConcrete"></typeparam>
        public void AddHostedService<TInterface, TConcrete>()
            where TInterface : class
            where TConcrete : class, TInterface, IHostedService
        {
            @this.AddSingleton<TConcrete>();

            @this.AddSingleton<IHostedService, TConcrete>(a => a.GetRequiredService<TConcrete>());
            @this.AddSingleton<TInterface>(a => a.GetRequiredService<TConcrete>());
        }
    }
}