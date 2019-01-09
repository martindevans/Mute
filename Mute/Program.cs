using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Mute.Context;
using Mute.Extensions;
using Mute.Services;
using Mute.Services.Audio;
using Mute.Services.Audio.Playback;
using Mute.Services.Games;
using Mute.Services.Responses;
using Newtonsoft.Json;
using Ninject;
using Ninject.Extensions.Conventions;

namespace Mute
{
    public class Program
    {
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;

        private readonly Configuration _config;

        #region static main
        private static int Main([NotNull] string[] args)
        {
            //Sanity check config file exists and early exit
            var configPath = args.Length < 1 ? "config.json" : args[0];
            if (!File.Exists(configPath))
            {
                Console.Write(Path.GetFullPath(configPath));
                Console.Error.WriteLine("No config file found");
                return -1;
            }

            //Read config file
            var config = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(configPath));

            //Run the program
            return new Program(config)
                .MainAsync(args)
                .GetAwaiter()
                .GetResult();
        }
        #endregion

        private Program([NotNull] Configuration config)
        {
            _config = config;

            _commands = new CommandService(new CommandServiceConfig {
                CaseSensitiveCommands = false,
                DefaultRunMode = RunMode.Async,
                ThrowOnError = true
            });
            _client = new DiscordSocketClient();

            var kernel = new StandardKernel();
            _services = kernel; 

            kernel.Bind<Random>().To<Random>().InTransientScope();
            kernel.Bind<IHttpClient>().To<MuteHttpClient>().InTransientScope();

            kernel.Bind<IServiceProvider>().ToConstant(kernel);
            kernel.Bind<Configuration>().ToConstant(_config);
            kernel.Bind<CommandService>().ToConstant(_commands);
            kernel.Bind<DiscordSocketClient>().ToConstant(_client);
            kernel.Bind<IDiscordClient>().ToConstant(_client);

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
                .GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .Where(t => typeof(IPreloadService).IsAssignableFrom(t))
                .Select(t => (IPreloadService)kernel.Get(t))
                .ToArray();
            Console.WriteLine("Preloading Services:");
            foreach (var preloadService in preload)
                Console.WriteLine($" - {preloadService.GetType().Name}");
        }

        private async Task<int> MainAsync(string[] args)
        {
            DependencyHelper.TestDependencies();

            await SetupModules();

            // Log the bot in
            await _client.LogoutAsync();
            await _client.LoginAsync(TokenType.Bot, _config.Auth.Token);
            await _client.StartAsync();

            // Set presence
            if (Debugger.IsAttached)
            {
                await _client.SetActivityAsync(new Game("Debug Mode"));
                await _client.SetStatusAsync(UserStatus.DoNotDisturb);
            }
            else
            {
                await _client.SetActivityAsync(null);
                await _client.SetStatusAsync(UserStatus.Online);
            }

            // Type `exit` to exit
            Console.WriteLine("type 'exit' to exit");
            while (true)
            {
                var line = Console.ReadLine();
                if (line != null && line.ToLowerInvariant() == "exit")
                {
                    await _client.LogoutAsync();
                    await _client.StopAsync();
                    _client.Dispose();
                    break;
                }
            }

            return 0;
        }

        private async Task SetupModules()
        {
            // Hook the MessageReceived Event into our Command Handler
            _client.MessageReceived += HandleMessage;

            // Discover all of the commands in this assembly and load them.
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            
            // Print loaded modules
            Console.WriteLine($"Loaded Modules ({_commands.Modules.Count()}):");
            foreach (var module in _commands.Modules)
                Console.WriteLine($" - {module.Name}");
        }

        private async Task HandleMessage(SocketMessage messageParam)
        {
            // Don't process the command if it was a System Message
            if (!(messageParam is SocketUserMessage message))
                return;

            //Ignore messages from self
            if (message.Author.Id == _client.CurrentUser.Id && !_config.ProcessMessagesFromSelf)
                return;

            // Check if the message starts with the command prefix character
            var prefixPos = 0;
            var hasPrefix = message.HasCharPrefix('!', ref prefixPos);

            // Create a context for this message
            var context = new MuteCommandContext(_client, message, _services);

            //Apply generic message preproccessor
            foreach (var pre in _services.GetServices<IMessagePreprocessor>())
                pre.Process(context);

            //Either process as command or try to process conversationally
            if (hasPrefix)
            {
                foreach (var pre in _services.GetServices<ICommandPreprocessor>())
                    pre.Process(context);
                await ProcessAsCommand(prefixPos, context);
            }
            else
            {
                foreach (var pre in _services.GetServices<IConversationPreprocessor>())
                    pre.Process(context);
                await _services.GetService<ConversationalResponseService>().Respond(context);
            }
        }

        private async Task ProcessAsCommand(int offset, [NotNull] ICommandContext context)
        {
            // When there's a mention the command may or may not include the prefix. Check if it does include it and skip over it if so
            if (context.Message.Content[offset] == '!')
                offset++;

            // Execute the command
            try
            {
                foreach (var pre in _services.GetServices<ICommandPreprocessor>())
                    pre.Process(context);

                var result = await _commands.ExecuteAsync(context, offset, _services);

                //Don't print error message in response to messages from self
                if (!result.IsSuccess && context.User.Id != _client.CurrentUser.Id)
                    await context.Channel.SendMessageAsync(result.ErrorReason);

                if (result.ErrorReason != null)
                    Console.WriteLine(result.ErrorReason);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
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