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
using Mute.Services;
using Mute.Services.Audio;
using Mute.Services.Games;
using Mute.Services.Responses;
using Newtonsoft.Json;

namespace Mute
{
    public class Program
    {
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;

        private readonly Configuration _config;

        #region static main
        private static void Main(string[] args) 
        {
            //Sanity check config file exists and early exit
            if (!File.Exists("config.json"))
            {
                Console.Write(Directory.GetCurrentDirectory());
                Console.Error.WriteLine("No config file found");
                return;
            }

            //Read config file
            var config = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText("config.json"));

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

            _commands = new CommandService(new CommandServiceConfig {
                CaseSensitiveCommands = false,
                DefaultRunMode = RunMode.Async,
                ThrowOnError = true
            });
            _client = new DiscordSocketClient();

            var serviceCollection = new ServiceCollection()
                .AddTransient<Random>()
                .AddTransient<IHttpClient>(_ => new MuteHttpClient())
                .AddSingleton(_config)
                .AddSingleton(_commands)
                .AddSingleton(_client)
                .AddSingleton(new DatabaseService(_config.Database))
                .AddSingleton<InteractiveService>()
                .AddSingleton<CatPictureService>()
                .AddSingleton<DogPictureService>()
                .AddSingleton<CryptoCurrencyService>()
                .AddSingleton(new AlphaAdvantageService(config.AlphaAdvantage))
                .AddSingleton<IouDatabaseService>()
                .AddSingleton<AudioPlayerService>()
                .AddSingleton<YoutubeService>()
                .AddSingleton<MusicRatingService>()
                .AddSingleton<GameService>()
                .AddSingleton<ReminderService>()
                .AddSingleton<SentimentService>()
                .AddSingleton<HistoryLoggingService>()
                .AddSingleton<ReactionSentimentTrainer>()
                .AddSingleton<ConversationalResponseService>();
            
            _services = serviceCollection.BuildServiceProvider();

            //Force creation of active services
            _services.GetService<GameService>();
            _services.GetService<ReminderService>();
            _services.GetService<SentimentService>();
            _services.GetService<HistoryLoggingService>();
            _services.GetService<ReactionSentimentTrainer>();
        }

        private async Task MainAsync(string[] args)
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
                await _client.SetActivityAsync(new Game("Debug Mode", ActivityType.Playing));
                await _client.SetStatusAsync(UserStatus.DoNotDisturb);
            }
            else
            {
                await _client.SetActivityAsync(null);
                await _client.SetStatusAsync(UserStatus.Online);
            }

            // Type exit to exit
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
        }

        private async Task SetupModules()
        {
            // Hook the MessageReceived Event into our Command Handler
            _client.MessageReceived += HandleMessage;

            // Discover all of the commands in this assembly and load them.
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
            
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
            if (message.Author.Id == _client.CurrentUser.Id)
                return;

            // Check if the message starts with the command prefix character
            var prefixPos = 0;
            var hasPrefix = message.HasCharPrefix('!', ref prefixPos);

            if (hasPrefix)
            {
                //It's a command, process it as such
                await ProcessAsCommand(message, prefixPos);
            }
            else
            {
                await _services.GetService<ConversationalResponseService>().Respond(message);
            }
        }

        private async Task ProcessAsCommand(SocketUserMessage message, int offset)
        {
            // Create a Command Context
            var context = new SocketCommandContext(_client, message);

            // When there's a mention the command may or may not include the prefix. Check if it does include it and skip over it if so
            if (context.Message.Content[offset] == '!')
                offset++;

            // Execute the command
            try
            {
                var result = await _commands.ExecuteAsync(context, offset, _services);

                //Don't print error message in response to messages from self
                if (!result.IsSuccess && message.Author.Id != _client.CurrentUser.Id)
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