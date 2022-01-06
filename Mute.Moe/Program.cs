using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Mute.Moe.Discord;
using Mute.Moe.Services.Host;
using Newtonsoft.Json;

namespace Mute.Moe
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            DependencyHelper.TestDependencies();

            var config = JsonConvert.DeserializeObject<Configuration>(await File.ReadAllTextAsync(string.Join(" ", args)));
            if (config == null)
                throw new InvalidOperationException("Config was null");

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

    internal class DependencyHelper
    {
        [DllImport("opus", EntryPoint = "opus_get_version_string", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr OpusVersionString();

        [DllImport("libsodium", EntryPoint = "sodium_version_string", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SodiumVersionString();
        
        public static void TestDependencies()
        {
            var opusVersion = Marshal.PtrToStringAnsi(OpusVersionString());
            Console.WriteLine($"Loaded opus with version string: {opusVersion}");

            var sodiumVersion = Marshal.PtrToStringAnsi(SodiumVersionString());
            Console.WriteLine($"Loaded sodium with version string: {sodiumVersion}");
        }
    }
}
