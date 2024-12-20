﻿using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Mute.Moe.Discord.Context;
using Mute.Moe.Discord.Context.Postprocessing;
using Mute.Moe.Discord.Context.Preprocessing;
using Mute.Moe.Discord.Services.Responses;
using ExecuteResult = Discord.Commands.ExecuteResult;
using IResult = Discord.Commands.IResult;
using RunMode = Discord.Commands.RunMode;

namespace Mute.Moe.Discord;

public class HostedDiscordBot
{
    private readonly Configuration _config;
    private readonly CommandService _commands;
    private readonly IServiceProvider _services;
    private readonly InteractionService _interactions;

    public DiscordSocketClient Client { get; }

    public HostedDiscordBot(DiscordSocketClient client, Configuration config, CommandService commands, IServiceProvider services, InteractionService interactions)
    {
        Client = client ?? throw new ArgumentNullException(nameof(client));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _commands = commands ?? throw new ArgumentNullException(nameof(commands));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _interactions = interactions ?? throw new ArgumentNullException(nameof(interactions));
    }

    public async Task StartAsync()
    {
        // Sanity check config
        if (_config.Auth == null)
            throw new InvalidOperationException("Cannot start bot: `Auth` section of config is null");
        if (_config.Auth.Token == null)
            throw new InvalidOperationException("Cannot start bot: `Auth.Token` in config is null");
        if (_config.Auth.ClientId == null)
            throw new InvalidOperationException("Cannot start bot: `Auth.ClientId` in config is null");

        // Discover all of the commands in this assembly and load them.
        try
        {
            await _commands.AddModulesAsync(Assembly.GetExecutingAssembly(), _services);
            await _interactions.AddModulesAsync(Assembly.GetExecutingAssembly(), _services);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        // Hook the MessageReceived Event into our Command Handler
        Client.MessageReceived += HandleMessage;
        _commands.CommandExecuted += CommandExecuted;

        // Hook up interactions
        Client.InteractionCreated += async x =>
        {
            var ctx = new SocketInteractionContext(Client, x);
            try
            {
                var result = await _interactions.ExecuteCommandAsync(ctx, _services);

                // Only pay attention to commands which fail due to an exception
                if (result.IsSuccess || result.Error is not InteractionCommandError.Exception)
                    return;
                throw new Exception(result.ErrorReason);
            }
            catch (Exception ex)
            {
                if (ctx.Interaction.HasResponded)
                    await ctx.Interaction.ModifyOriginalResponseAsync(props => props.Content = ex.Message);
                else
                    await ctx.Interaction.RespondAsync(ex.Message);
            }
        };

        var tcs = new TaskCompletionSource<bool>();
        Client.Ready += () => {
            tcs.SetResult(true);
            return Task.CompletedTask;
        };

        // Log the bot in
        await Client.LogoutAsync();
        await Client.LoginAsync(TokenType.Bot, _config.Auth.Token);
        await Client.StartAsync();

        // Precache all users in all guilds
        await Client.DownloadUsersAsync(Client.Guilds);

        // Set presence
        if (Debugger.IsAttached)
        {
            await Client.SetActivityAsync(new CustomStatusGame("Debug Mode"));
            await Client.SetStatusAsync(UserStatus.DoNotDisturb);
        }
        else
        {
            await Client.SetActivityAsync(null);
            await Client.SetStatusAsync(UserStatus.Online);
        }

        // Wait for ready
        await tcs.Task;
    }

    private static async Task CommandExecuted(Optional<CommandInfo> command, ICommandContext context,  IResult result)
    {
        // Only pay attention to commands which fail due to an exception
        if (result.IsSuccess || result.Error is not CommandError.Exception)
            return;

        if (result is ExecuteResult er)
            Console.WriteLine(er.Exception);

        await context.Channel.SendMessageAsync("Command Exception! " + result.ErrorReason);
    }

    public async Task StopAsync()
    {
        await Client.LogoutAsync();
        await Client.StopAsync();
    }

    private async Task HandleMessage(SocketMessage socketMessage)
    {
        try
        {
            // Don't process the command if it was a System Message
            if (socketMessage is not SocketUserMessage message)
                return;

            // Ignore messages from self
            if (message.Author.Id == Client.CurrentUser.Id && !_config.ProcessMessagesFromSelf)
                return;

            // Check if the message starts with the command prefix character
            var prefixPos = 0;
            var hasPrefix = message.HasCharPrefix(_config.PrefixCharacter, ref prefixPos);

            // Create a context for this message
            await using var context = new MuteCommandContext(Client, message, _services);

            // Apply generic message preproccessor
            var preprocessors = _services.GetServices<IMessagePreprocessor>().ToList();
            foreach (var pre in preprocessors)
            {
                try
                {
                    await pre.Process(context);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }

            // Either process as command or try to process conversationally
            if (hasPrefix)
            {
                await ProcessAsCommand(prefixPos, context);
            }
            else
            {
                foreach (var pre in _services.GetServices<IConversationPreprocessor>())
                    await pre.Process(context);
                await _services.GetRequiredService<ConversationalResponseService>().Respond(context);
            }
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync($"Message handler threw: {ex.Message}");
            throw;
        }
    }

    private async Task ProcessAsCommand(int offset, MuteCommandContext context)
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
                foreach (var post in _services.GetServices<IUnsuccessfulCommandPostprocessor>().OrderBy(a => a.Order))
                    if (await post.Process(context, result))
                        break;
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
            ThrowOnError = true,
        }));

        var client = new DiscordSocketClient(new DiscordSocketConfig {
            AlwaysDownloadUsers = true,
            GatewayIntents = GatewayIntents.All,
        });

        services.AddSingleton(new InteractionService(client.Rest, new InteractionServiceConfig
        {
            DefaultRunMode = global::Discord.Interactions.RunMode.Async,
            UseCompiledLambda = true,
        }));

        services.AddSingleton(client);
        services.AddSingleton<BaseSocketClient>(client);
        services.AddSingleton<BaseDiscordClient>(client);
        services.AddSingleton<IDiscordClient>(client);

        services.AddTransient<IUnsuccessfulCommandPostprocessor, DisplayCommandError>();
    }
}