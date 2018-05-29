using System;
using System.Collections.Generic;
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
using Newtonsoft.Json;

namespace Mute
{
    class Program
    {
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;

        private readonly Configuration _config;

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

        public Program([NotNull] Configuration config)
        {
            _config = config;

            _commands = new CommandService(new CommandServiceConfig {
                CaseSensitiveCommands = false,
                DefaultRunMode = RunMode.Async
            });
            _client = new DiscordSocketClient();

            var serviceCollection = new ServiceCollection()
                .AddSingleton(_config)
                .AddSingleton(_commands)
                .AddSingleton(_client)
                .AddSingleton(new DatabaseService(_config.Database))
                .AddSingleton<InteractiveService>()
                .AddSingleton<CatPictureService>()
                .AddSingleton<DogPictureService>()
                .AddSingleton<CryptoCurrencyService>()
                .AddSingleton<IStockService>(new AlphaAdvantageService(config.AlphaAdvantage))
                .AddSingleton<IouDatabaseService>()
                .AddSingleton<FileSystemService>()
                .AddSingleton<AudioPlayerService>()
                .AddSingleton<YoutubeService>()
                .AddScoped<Random>();

            _services = serviceCollection.BuildServiceProvider();
        }

        public async Task MainAsync(string[] args)
        {
            DependencyHelper.TestDependencies();

            await SetupModules();

            // Log the bot in
            await _client.LogoutAsync();
            await _client.LoginAsync(TokenType.Bot, _config.Auth.Token);
            await _client.StartAsync();

            // Set presence
            if (Debugger.IsAttached)
                await _client.SetGameAsync("Debug Mode");

            // Type exit to exit
            Console.WriteLine("type 'exit' to exit");
            while (true)
            {
                var line = Console.ReadLine();
                if (line != null && line.ToLowerInvariant() == "exit")
                {
                    await _client.LogoutAsync();
                    await _client.StopAsync();
                    break;
                }
            }
        }

        public async Task SetupModules()
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

        public async Task HandleMessage(SocketMessage messageParam)
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

            // Check if the bot is mentioned in a prefix
            var prefixMentionPos = 0;
            var hasPrefixMention = message.HasMentionPrefix(_client.CurrentUser, ref prefixMentionPos);

            // Check if the bot is mentioned at all
            var mentionsBot = ((IUserMessage)message).MentionedUserIds.Contains(_client.CurrentUser.Id);

            if (hasPrefix || hasPrefixMention)
            {
                //It's a command, process it as such
                await ProcessAsCommand(message, Math.Max(prefixPos, prefixMentionPos));
            }
            else if (mentionsBot)
            {
                //It's not a command, but the bot was mentioned
                Console.WriteLine($"I was mentioned in: '{message.Content}'");
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