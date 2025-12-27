using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using Mute.Moe.Discord;
using Mute.Moe.Services.Host;
using Mute.Moe.Tools;
using Newtonsoft.Json;
using Serilog;

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
        DependencyHelper.TestDependencies();

        var config = JsonConvert.DeserializeObject<Configuration>(await File.ReadAllTextAsync(string.Join(" ", args)))
                  ?? throw new InvalidOperationException("Config was null");

        // Setup logger
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        // Build DI container
        var collection = new ServiceCollection();
        collection.AddSingleton<ServiceHost>();
        var startup = new Startup(config);
        startup.ConfigureServices(collection);
        var provider = collection.BuildServiceProvider();

        // Run some service setup stuff
        await provider.GetRequiredService<IToolIndex>().Update();

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

        WaitForExitSignal();

        await bot.StopAsync();
        await provider.GetRequiredService<ServiceHost>().StopAsync(default);
    }

    /// <summary>
    /// Wait for an exit signal to terminate the application
    /// </summary>
    private static void WaitForExitSignal()
    {
        Log.Information("Type 'exit' to exit");

        while (true)
        {
            var line = Console.ReadLine();
            if (line != null && line.Equals("exit", StringComparison.InvariantCultureIgnoreCase))
                return;
        }
    }
}

internal static partial class DependencyHelper
{
    [LibraryImport("opus", EntryPoint = "opus_get_version_string")]
    [UnmanagedCallConv(CallConvs = [ typeof(System.Runtime.CompilerServices.CallConvCdecl) ])]
    private static partial nint OpusVersionString();

    [LibraryImport("libsodium", EntryPoint = "sodium_version_string")]
    [UnmanagedCallConv(CallConvs = [ typeof(System.Runtime.CompilerServices.CallConvCdecl) ])]
    private static partial nint SodiumVersionString();
        
    public static void TestDependencies()
    {
        // These calls are important, they load the string from the DLL. So successfully making
        // these calls indicates that the deps are loaded.

        var opusVersion = Marshal.PtrToStringAnsi(OpusVersionString());
        Log.Information("Loaded opus: {0}", opusVersion);

        var sodiumVersion = Marshal.PtrToStringAnsi(SodiumVersionString());
        Log.Information("Loaded sodium: {0}", sodiumVersion);
    }
}