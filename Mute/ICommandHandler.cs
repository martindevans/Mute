using System;
using System.Threading.Tasks;
using Discord.Commands;
using System.Linq;
using System.Reflection;
using Discord;
using System.Diagnostics;
using Discord.WebSocket;

namespace Mute
{
    public interface ICommandHandler
    {
        Task MainAsync(string[] args);
        Task SetupModules();
        Task HandleMessage(SocketMessage messageParam);
        Task ProcessAsCommand(SocketUserMessage message, int offset);
    }

    // Just let us speak to mute locally.
    public class LocalCommandHandler : ICommandHandler
    {
        private readonly IServiceProvider _services;

        public LocalCommandHandler(LocalProviderConfiguration config, IServiceProvider services)
        {
            _services = services;
        }

        public Task HandleMessage(SocketMessage messageParam)
        {
            throw new NotImplementedException();
        }

        public Task MainAsync(string[] args)
        {
            throw new NotImplementedException();
        }

        public Task ProcessAsCommand(SocketUserMessage message, int offset)
        {
            throw new NotImplementedException();
        }

        public Task SetupModules()
        {
            throw new NotImplementedException();
        }
    }

    public class DiscordCommandHandler : ICommandHandler
    {
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _client;
        private readonly DiscordProviderConfiguration _config;
        private readonly IServiceProvider _services;

        public DiscordCommandHandler(CommandServiceConfig svcConfig, DiscordProviderConfiguration config, IServiceProvider services) {
            // Literally just straight up instantiate the discord varient here.
            _commands = new CommandService(svcConfig);
            _client = new DiscordSocketClient();
            _config = config;
            _services = services;
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

        public async Task MainAsync(string[] args)
        {
            DependencyHelper.TestDependencies();

            await SetupModules();

            // Log the bot in
            await _client.LogoutAsync();
            await _client.LoginAsync(TokenType.Bot, _config.Token);
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
                    _client.Dispose();
                    break;
                }
            }
        }

        public async Task ProcessAsCommand(SocketUserMessage message, int offset)
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

        public async Task SetupModules()
        {
            // Hook the MessageReceived Event into our Command Handler
            _client.MessageReceived += HandleMessage;

            // Discover all of the commands in this assembly and load them.
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());

            // Print loaded modules
            //Console.WriteLine($"Loaded Modules ({_commands.Modules.Count()}):");
            foreach (var module in _commands.Modules)
                Console.WriteLine($" - {module.Name}");
        }
    }
}
