using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using CommandLine;
using Discord.Addons.Interactive;
using JetBrains.Annotations;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Mute.Moe.Discord;
using Mute.Moe.Discord.Services;
using Mute.Moe.Discord.Services.Audio;
using Mute.Moe.Discord.Services.Audio.Playback;
using Mute.Moe.Discord.Services.Games;
using Mute.Moe.Discord.Services.Responses;
using Mute.Moe.Services;
using Mute.Moe.Services.Database;
using Mute.Moe.Services.Images;
using Mute.Moe.Services.Search;
using Mute.Moe.Services.Sentiment;
using Newtonsoft.Json;

namespace Mute.Moe
{
    public class Program
    {
        public static void Main(string[] args)
        {
            DependencyHelper.TestDependencies();

            Parser
                .Default
                .ParseArguments<Options>(args)
                .WithNotParsed(ParsingError)
                .WithParsed(CreateStartupSuccess(args));
        }

        [NotNull] private static Action<Options> CreateStartupSuccess(string[] args)
        {
            return options =>
            {
                if (!File.Exists(options.ConfigPath))
                {
                    Console.Write(Path.GetFullPath(options.ConfigPath));
                    Console.Error.WriteLine("No config file found");
                    return;
                }

                var config = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(options.ConfigPath));

                var host = WebHost.CreateDefaultBuilder(args)
                       .ConfigureServices(a => ConfigureBaseServices(a, config))
                       .ConfigureServices(HostedDiscordBot.ConfigureServices)
                       .ConfigureServices(a => a.AddHostedService<HostedDiscordBot>())
                       .ConfigureServices(a => a.AddHostedService<ServicePreloader>())
                       .ConfigureServices(a => a.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                       .AddCookie("Cookies")
                       .AddDiscord(d => {
                           d.AppId = config.Auth.ClientId;
                           d.AppSecret = config.Auth.ClientSecret;
                           d.Scope.Add("identify");
                           d.SaveTokens = true;
                       }))
                       .UseStartup<Startup>()
                       .Build();

                var cts = new CancellationTokenSource();
                var webhost = host.RunAsync(cts.Token);

                WaitForExitSignal();

                cts.Cancel();
                webhost.GetAwaiter().GetResult();
            };
        }

        /// <summary>
        /// Handle an error in parsing command line args
        /// </summary>
        /// <param name="errors"></param>
        private static void ParsingError([NotNull] IEnumerable<Error> errors)
        {
            Console.WriteLine($"Failed to start application due to {errors.Count()} errors:");
            foreach (var error in errors)
                Console.WriteLine($" - {error}");
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

        private static void ConfigureBaseServices(IServiceCollection services, Configuration config)
        {
            services.AddTransient<Random>();

            services
                .AddSingleton(config)
                .AddSingleton(services);

            services.AddSingleton<InteractiveService>();

            services.AddSingleton<IHttpClient, SimpleHttpClient>();
            services.AddSingleton<IDatabaseService, SqliteDatabase>();
            services.AddSingleton<ISentimentService, TensorflowSentiment>();
            services.AddSingleton<ICatPictureService, CataasPictures>();
            services.AddSingleton<IDogPictureService, DogceoPictures>();
            services.AddSingleton<IAnimeSearch, NadekobotAnimeSearch>();

            //Eventually these should all become interface -> concrete type bindings
            services
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
        }
    }

    /// <summary>
    /// Describe options to be passed in by command line switches
    /// </summary>
    public class Options
    {
        [Option('c', "config", Required = true, HelpText = "Path to the config file")]
        public string ConfigPath { get; set; }

        [Option('d', "database", Required = true, HelpText = "Path to the database file")]
        public string DatabasePath { get; set; }
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
