using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Mute.Moe.Extensions;

/// <summary>
/// Extensions to <see cref="IServiceCollection"/>
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Scans the specified assemblies for types that implement the given interface and registers them with the specified lifetime.
    /// </summary>
    /// <param name = "services" > The IServiceCollection to add the services to.</param>
    /// <param name = "interfaceType" > The interface type to scan for (e.g., typeof(IToolProvider)).</param>
    /// <param name = "assemblies" > The assemblies to scan.If not provided, it scans the calling assembly.</param>
    /// <param name = "lifetime" > The service lifetime(Singleton, Scoped, or Transient). Defaults to Transient.</param>
    public static void RegisterImplementationsOf(this IServiceCollection services, Type interfaceType, IEnumerable<Assembly> assemblies, ServiceLifetime lifetime)
    {
        // Find all concrete, non-abstract types that implement the specified interface
        var implementationTypes = from assembly in assemblies
                                  from type in assembly.GetTypes()
                                  where interfaceType.IsAssignableFrom(type)
                                  where !type.IsInterface
                                  where !type.IsAbstract
                                  select type;

        // Register the type with the DI container under its interface.
        foreach (var implementationType in implementationTypes)
        {
            switch (lifetime)
            {
                case ServiceLifetime.Singleton:
                    services.AddSingleton(interfaceType, implementationType);
                    break;

                case ServiceLifetime.Scoped:
                    services.AddScoped(interfaceType, implementationType);
                    break;

                case ServiceLifetime.Transient:
                    services.AddTransient(interfaceType, implementationType);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
            }
        }
    }
}