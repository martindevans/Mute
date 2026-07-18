using System.Diagnostics.CodeAnalysis;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using Mute.Moe.Discord;
using Mute.Moe.Services.Host;
using Newtonsoft.Json;
using OpenTelemetry.Trace;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Mute.Moe;

/// <summary>
/// Root entry point of *Mute
/// </summary>
public static class Program
{
    /// <summary>
    /// Root entry point of *Mute
    /// </summary>
    public static async Task Main(string[] args)
    {
        // Check native deps can be loaded, throws if not
        DependencyHelper.TestDependencies();

        // Load config file
        var config = JsonConvert.DeserializeObject<Configuration>(await File.ReadAllTextAsync(string.Join(" ", args)))
                  ?? throw new InvalidOperationException("Config was null");

        Console.WriteLine("### Version: 14");

        // Build DI container
        var collection = new ServiceCollection();
        new Startup(config).ConfigureServices(collection);
        var provider = collection.BuildServiceProvider();

        // Tracer provider must be initialised before we do any tracing/telemetry! Get the
        // service right now to ensure it's initialised.
        provider.GetRequiredService<TracerProvider>();

        // Connect to Discord
        var bot = provider.GetRequiredService<HostedDiscordBot>();
        await bot.StartAsync();

        // Start long running services
        await provider.GetRequiredService<ServiceHost>().StartAsync(default);

        // Register interactions. If this is debug mode only register them to the test guild
        var interactions = provider.GetRequiredService<InteractionService>();
#if DEBUG
        await interactions.RegisterCommandsToGuildAsync(537765528991825920); // Nadeko Test
#else
        await interactions.RegisterCommandsGloballyAsync(true);
#endif

        // Wait for "exit" to be typed in
        WaitForExitSignal();

        // Disconnect from Discord
        await bot.StopAsync();
        
        // Stop long running services
        await provider.GetRequiredService<ServiceHost>().StopAsync(default);
    }

    /// <summary>
    /// Wait for an exit signal to terminate the application
    /// </summary>
    private static void WaitForExitSignal()
    {
        Console.WriteLine("### Type 'exit' to exit ###");

        while (true)
        {
            var line = Console.ReadLine();
            if (line != null && line.Equals("exit", StringComparison.InvariantCultureIgnoreCase))
                return;
        }
    }
}

[ExcludeFromCodeCoverage]
internal static partial class DependencyHelper
{
    [LibraryImport("libsodium", EntryPoint = "sodium_version_string")]
    [UnmanagedCallConv(CallConvs = [ typeof(System.Runtime.CompilerServices.CallConvCdecl) ])]
    private static partial nint SodiumVersionString();
        
    public static void TestDependencies()
    {
        // These calls are important, they load the string from the DLL. So successfully making
        // these calls indicates that the deps are loaded.

        var sodiumVersion = Marshal.PtrToStringAnsi(SodiumVersionString());
        //Log.Information("Loaded sodium: {0}", sodiumVersion);
    }
}