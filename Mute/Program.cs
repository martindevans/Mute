using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.WebSocket;
using JetBrains.Annotations;
using Mute.Extensions;
using Mute.Services;
using Mute.Services.Audio;
using Mute.Services.Audio.Playback;
using Mute.Services.Games;
using Mute.Services.Responses;
using Newtonsoft.Json;
using Ninject;

namespace Mute
{
    public class Program
    {
        private readonly Configuration _config;
        private readonly StandardKernel _services;
        private readonly DiscordBot _bot;

        public static void Main([NotNull] string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        private static async Task MainAsync([NotNull] IReadOnlyList<string> args)
        {
            DependencyHelper.TestDependencies();

            //Sanity check config file exists and early exit
            var configPath = args.Count < 1 ? "config.json" : args[0];
            if (!File.Exists(configPath))
            {
                Console.Write(Path.GetFullPath(configPath));
                Console.Error.WriteLine("No config file found");
            }
            else
            {
                var config = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(configPath));
                await new Program(config).Run();
            }
        }

        public Program(Configuration config)
        {
            _config = config;
            _services = new StandardKernel();

            _bot = new DiscordBot(_config, _services);
        }

        private async Task Run()
        {
            var kernel = _services;
            kernel.Bind<Random>().To<Random>().InTransientScope();
            kernel.Bind<IHttpClient>().To<MuteHttpClient>().InTransientScope();

            kernel.Bind<IServiceProvider>().ToConstant(kernel);
            kernel.Bind<Configuration>().ToConstant(_config);

            kernel.Bind<IDatabaseService>().To<DatabaseService>().InSingletonScope();
            kernel.Bind<ISentimentService>().To<TensorflowSentimentService>().InSingletonScope();
            kernel.Bind<InteractiveService>().ToConstructor(a => new InteractiveService(a.Inject<DiscordSocketClient>(), null)).InSingletonScope();

            kernel
                .AddSingleton<CatPictureService>()
                .AddSingleton<DogPictureService>()
                .AddSingleton<CryptoCurrencyService>()
                .AddSingleton<AlphaAdvantageService>()
                .AddSingleton<IouDatabaseService>()
                .AddSingleton<MusicPlayerService>()
                .AddSingleton<YoutubeService>()
                .AddSingleton<MusicRatingService>()
                .AddSingleton<GameService>()
                .AddSingleton<ReminderService>()
                .AddSingleton<SentimentTrainingService>()
                .AddSingleton<HistoryLoggingService>()
                .AddSingleton<ReactionSentimentTrainer>()
                .AddSingleton<ConversationalResponseService>()
                .AddSingleton<WikipediaService>()
                .AddSingleton<TimeService>()
                .AddSingleton<SteamApi>()
                .AddSingleton<SoundEffectService>()
                .AddSingleton<WordsService>()
                .AddSingleton<SpacexService>()
                .AddSingleton<WordVectorsService>()
                .AddSingleton<UptimeService>()
                .AddSingleton<WordTrainingService>()
                .AddSingleton<RoleService>()
                .AddSingleton<MultichannelAudioService>();

            //Find all types which implement IPreloadService and load them
            var preload = Assembly
                .GetAssembly(typeof(DiscordBot))
                .GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => typeof(IPreloadService).IsAssignableFrom(t))
                .Select(t => (IPreloadService)kernel.Get(t))
                .ToArray();
            Console.WriteLine("Preloading Services:");
            foreach (var preloadService in preload)
                Console.WriteLine($" - {preloadService.GetType().Name}");

            //Create a cancellation source we'll use to kill things
            var cts = new CancellationTokenSource();

            //Start the bot (running until cancellation token says so)
            await _bot.Setup();
            var runningBot = _bot.Start(cts.Token);

            //Loop reading from console. Once `exit` is typed, set the cancellation token
            Console.WriteLine("type 'exit' to exit");
            while (true)
            {
                var line = Console.ReadLine();
                if (line != null && line.ToLowerInvariant() == "exit")
                {
                    cts.Cancel();

                    await runningBot;
                    return;
                }
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
