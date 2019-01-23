using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Mute.Moe
{
    public class Program
    {
        public static void Main(string[] args)
        {
            DependencyHelper.TestDependencies();

            var host = WebHost.CreateDefaultBuilder(args)
                              .UseStartup<Startup>()
                              .UseKestrel()
                              .Build();

            var cts = new CancellationTokenSource();
            var webhost = host.RunAsync(cts.Token);

            WaitForExitSignal();

            cts.Cancel();
            webhost.GetAwaiter().GetResult();
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
