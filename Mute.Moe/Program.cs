using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using Mute.Moe.Discord;
using Mute.Moe.Services.Host;
using Newtonsoft.Json;

namespace Mute.Moe;

public class Program
{
    public static async Task Main(string[] args)
    {
        DependencyHelper.TestDependencies();

        var config = JsonConvert.DeserializeObject<Configuration>(await File.ReadAllTextAsync(string.Join(" ", args)))
                  ?? throw new InvalidOperationException("Config was null");

        var collection = new ServiceCollection();
        collection.AddSingleton<ServiceHost>();
        var startup = new Startup(config);
        startup.ConfigureServices(collection);
        var provider = collection.BuildServiceProvider();

        // Connect to Discord
        var bot = provider.GetRequiredService<HostedDiscordBot>();
        await bot.StartAsync();

        // Get information about a guild, when this completes it means the bot is in a sensible state to start other services
        await bot.Client.Rest.GetGuildAsync(415655090842763265);
        await Task.Delay(1000);
        await provider.GetRequiredService<ServiceHost>().StartAsync(default);

        // Register interactions. If this is debug mode only register them to the test guild
        var interactions = provider.GetRequiredService<InteractionService>();
#if DEBUG
        await interactions.RegisterCommandsToGuildAsync(537765528991825920); // Nadeko Test
        await interactions.RegisterCommandsToGuildAsync(415655090842763265); // Lightbulb Appreciation Society
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
        Console.WriteLine("type 'exit' to exit");
        while (true)
        {
            var line = Console.ReadLine();
            if (line != null && line.ToLowerInvariant() == "exit")
                return;
        }
    }
}

internal partial class DependencyHelper
{
    [LibraryImport("opus", EntryPoint = "opus_get_version_string")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static partial nint OpusVersionString();

    [LibraryImport("libsodium", EntryPoint = "sodium_version_string")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
    private static partial nint SodiumVersionString();
        
    public static void TestDependencies()
    {
        var opusVersion = Marshal.PtrToStringAnsi(OpusVersionString());
        Console.WriteLine($"Loaded opus with version string: {opusVersion}");

        var sodiumVersion = Marshal.PtrToStringAnsi(SodiumVersionString());
        Console.WriteLine($"Loaded sodium with version string: {sodiumVersion}");
    }
}