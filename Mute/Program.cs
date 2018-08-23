using System;
using System.Collections.Generic;

using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Mute.Services;
using Mute.Services.Audio;
using Mute.Services.Conversation;
using Mute.Services.Games;
using Newtonsoft.Json;

namespace Mute
{
    class Program
    {
        private readonly ICommandHandler _commands;
        private readonly IServiceProvider _services;
        private readonly Configuration _config;
        private List<ICommandHandler> _handlers;

        #region static main
        static void Main(string[] args) 
        {
            //Sanity check config file exists and early exit
            if (!File.Exists(@"config.json"))
            {
                Console.Write(Directory.GetCurrentDirectory());
                Console.Error.WriteLine("No config file found");
                return;
            }

            //Read config file
            var config = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(@"config.json"));

            //Run the program
            new Program(config)
                .MainAsync(args)
                .GetAwaiter()
                .GetResult();
        }
        #endregion

        private Program([NotNull] Configuration config)
        {
            _config = config;
            List<ICommandHandler> handlers = new List<ICommandHandler>();

            var serviceCollection = new ServiceCollection()
                .AddScoped<Random>()
                .AddSingleton(_config)
                .AddSingleton(new DatabaseService(_config.Database))
                .AddSingleton<InteractiveService>()
                .AddSingleton<CatPictureService>()
                .AddSingleton<DogPictureService>()
                .AddSingleton<CryptoCurrencyService>()
                .AddSingleton<IStockService>(new AlphaAdvantageService(config.AlphaAdvantage))
                .AddSingleton<IouDatabaseService>()
                .AddSingleton<AudioPlayerService>()
                .AddSingleton<YoutubeService>()
                .AddSingleton<MusicRatingService>()
                .AddSingleton<GameService>()
                .AddSingleton<GreetingService>();

            _services = serviceCollection.BuildServiceProvider();

            //Force creation of active services
            _services.GetService<GameService>();
            _services.GetService<GreetingService>();

            // Populate our providers
            if (_config.Handler.Discord != null)
            {
                handlers.Add(
                    new DiscordCommandHandler(new CommandServiceConfig
                    {
                        CaseSensitiveCommands = false,
                        DefaultRunMode = RunMode.Async,
                        ThrowOnError = true
                    }, 
                    _config.Handler.Discord,
                    _services)
                );
            }

            if (_config.Handler.Local != null)
            {
                handlers.Add(
                    new LocalCommandHandler(
                        _config.Handler.Local,
                        _services
                        ));
            }

            if (handlers.Count == 0)
            {
                // No providers given, so lets blow up
                throw new Exception("No Handlers given in config.");
            }

            _handlers = handlers;
        }

        public Task MainAsync(string[] args)
        {
            // Yolo, how do threads even work in C#
            List<TaskAwaiter> _awaiters = new List<TaskAwaiter>();
            for (var i = 0; i < _handlers.Count; i++)
            {
                // go _providers[i].Main();
                _awaiters.Add(_handlers[i].MainAsync(
                    new string[0]).GetAwaiter());
            }

            for (var a = 0; a < _awaiters.Count; a++)
            {
                _awaiters[a].GetResult();
            }

            // Yeah, Fuck yeah
            // Im sure this wont come back to bite me later
            return null;
        }
    }

    class DependencyHelper
    {
        [DllImport("opus", EntryPoint = "opus_get_version_string", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr OpusVersionString();
        [DllImport("libsodium", EntryPoint = "sodium_version_string", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SodiumVersionString();
        
        public static void TestDependencies()
        {
            string opusVersion = Marshal.PtrToStringAnsi(OpusVersionString());
            Console.WriteLine($"Loaded opus with version string: {opusVersion}");
            string sodiumVersion = Marshal.PtrToStringAnsi(SodiumVersionString());
            Console.WriteLine($"Loaded sodium with version string: {sodiumVersion}");
        }
    }
}