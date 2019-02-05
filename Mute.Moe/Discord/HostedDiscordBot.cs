using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mute.Moe.Discord.Context;
using Mute.Moe.Discord.Context.Postprocessing;
using Mute.Moe.Discord.Context.Preprocessing;
using Mute.Moe.Discord.Services.Responses;

namespace Mute.Moe.Discord
{
    public class HostedDiscordBot
        : IHostedService
    {
        private readonly DiscordSocketClient _client;
        private readonly Configuration _config;
        private readonly CommandService _commands;
        private readonly IServiceProvider _services;

        public HostedDiscordBot(DiscordSocketClient client, Configuration config, CommandService commands, IServiceProvider services)
        {
            _client = client;
            _config = config;
            _commands = commands;
            _services = services;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Discover all of the commands in this assembly and load them.
            await _commands.AddModulesAsync(Assembly.GetExecutingAssembly(), _services);

            // Hook the MessageReceived Event into our Command Handler
            _client.MessageReceived += HandleMessage;

            _commands.CommandExecuted += CommandExecuted;

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
        }

        private async Task CommandExecuted(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {

        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _client.LogoutAsync();
            await _client.StopAsync();
        }

        [NotNull] private async Task HandleMessage([NotNull] SocketMessage socketMessage)
        {
            // Don't process the command if it was a System Message
            if (!(socketMessage is SocketUserMessage message))
                return;

            //Ignore messages from self
            if (message.Author.Id == _client.CurrentUser.Id && !_config.ProcessMessagesFromSelf)
                return;

            // Check if the message starts with the command prefix character
            var prefixPos = 0;
            var hasPrefix = message.HasCharPrefix(_config.PrefixCharacter, ref prefixPos);

            // Create a context for this message
            var context = new MuteCommandContext(_client, message, _services);

            //Apply generic message preproccessor
            foreach (var pre in _services.GetServices<IMessagePreprocessor>())
                await pre.Process(context);

            //Either process as command or try to process conversationally
            if (hasPrefix)
            {
                foreach (var pre in _services.GetServices<ICommandPreprocessor>())
                    await pre.Process(context);
                await ProcessAsCommand(prefixPos, context);
            }
            else
            {
                foreach (var pre in _services.GetServices<IConversationPreprocessor>())
                    await pre.Process(context);
                await _services.GetService<ConversationalResponseService>().Respond(context);
            }
        }

        private async Task ProcessAsCommand(int offset, [NotNull] MuteCommandContext context)
        {
            // When there's a mention the command may or may not include the prefix. Check if it does include it and skip over it if so
            if (context.Message.Content[offset] == _config.PrefixCharacter)
                offset++;

            // Execute the command
            try
            {
                foreach (var pre in _services.GetServices<ICommandPreprocessor>())
                    await pre.Process(context);

                var result = await _commands.ExecuteAsync(context, offset, _services);
                if (result.IsSuccess)
                {
                    foreach (var post in _services.GetServices<ISuccessfulCommandPostprocessor>())
                        await post.Process(context);
                }
                else
                {
                    foreach (var post in _services.GetServices<IUnsuccessfulCommandPostprocessor>())
                        await post.Process(context, result);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(new CommandService(new CommandServiceConfig {
                CaseSensitiveCommands = false,
                DefaultRunMode = RunMode.Async,
                ThrowOnError = true
            }));

            var client = new DiscordSocketClient(new DiscordSocketConfig {
                AlwaysDownloadUsers = true
            });

            services.AddSingleton(client);
            services.AddSingleton<IDiscordClient>(client);

            services.AddTransient<IUnsuccessfulCommandPostprocessor, DisplayCommandError>();
        }
    }
}
