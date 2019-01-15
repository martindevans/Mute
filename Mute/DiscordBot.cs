using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Mute.Context;
using Mute.Services.Responses;
using Ninject;

namespace Mute
{
    public class DiscordBot
    {
        private readonly Configuration _config;
        private readonly IKernel _services;
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        
        public DiscordBot(Configuration config, IKernel services)
        {
            _commands = new CommandService(new CommandServiceConfig {
                CaseSensitiveCommands = false,
                DefaultRunMode = RunMode.Async,
                ThrowOnError = true
            });
            _client = new DiscordSocketClient();
            _config = config;
            _services = services;
        }

        public async Task Setup()
        {
            _services.Bind<CommandService>().ToConstant(_commands);
            _services.Bind<DiscordSocketClient>().ToConstant(_client);
            _services.Bind<IDiscordClient>().ToConstant(_client);

            // Discover all of the commands in this assembly and load them.
            await _commands.AddModulesAsync(Assembly.GetExecutingAssembly(), _services);
            Console.WriteLine($"Loaded Modules ({_commands.Modules.Count()}):");
            foreach (var module in _commands.Modules)
                Console.WriteLine($" - {module.Name}");

            // Hook the MessageReceived Event into our Command Handler
            _client.MessageReceived += HandleMessage;
        }

        public async Task Start(CancellationToken token)
        {
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

            //Create a task completion source that will hang until cancelled
            var tcs = new TaskCompletionSource<int>();

            //When token is cancelled kill the client and cancel the long running task
            token.Register(async () => {
                await _client.LogoutAsync();
                await _client.StopAsync();
                tcs.SetResult(0);
            });

            //Make this task persist until bot is terminated
            await tcs.Task;
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
}
