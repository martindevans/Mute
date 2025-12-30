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
using Serilog;
using Serilog.Events;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using ExecuteResult = Discord.Commands.ExecuteResult;
using IResult = Discord.Commands.IResult;
using RunMode = Discord.Commands.RunMode;

namespace Mute.Moe.Discord;

/// <summary>
/// Connects to discord, receives events, dispatches them to various bot systems. Really the heart of the bot.
/// </summary>
public class HostedDiscordBot
{
    private readonly Configuration _config;
    private readonly CommandService _commands;
    private readonly IServiceProvider _services;
    private readonly InteractionService _interactions;

    /// <summary>
    /// The <see cref="DiscordSocketClient"/> in use
    /// </summary>
    public DiscordSocketClient Client { get; }

    /// <summary>
    /// Create new <see cref="HostedDiscordBot"/>
    /// </summary>
    /// <param name="client"></param>
    /// <param name="config"></param>
    /// <param name="commands"></param>
    /// <param name="services"></param>
    /// <param name="interactions"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public HostedDiscordBot(DiscordSocketClient client, Configuration config, CommandService commands, IServiceProvider services, InteractionService interactions)
    {
        Client = client ?? throw new ArgumentNullException(nameof(client));

        _config = config ?? throw new ArgumentNullException(nameof(config));
        _commands = commands ?? throw new ArgumentNullException(nameof(commands));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _interactions = interactions ?? throw new ArgumentNullException(nameof(interactions));

        Client.Log += LogAsync;
    }

    /// <summary>
    /// Start the bot
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
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
            Log.Error(e, "Exception while adding modules and interactions");
            throw;
        }

        // Hook the MessageReceived Event into our Command Handler
        Client.MessageReceived += HandleMessage;
        _commands.CommandExecuted += CommandExecuted;

        // Hook up interactions
        Client.InteractionCreated += async interaction =>
        {
            Log.Information("InteractionCreated Start: {0}", interaction.Id);

            var ctx = new SocketInteractionContext(Client, interaction);
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
                Log.Error("InteractionCreated Error: {0}: {1}", interaction.Id, ex);

                if (ctx.Interaction.HasResponded)
                    await ctx.Interaction.ModifyOriginalResponseAsync(props => props.Content = ex.Message);
                else
                    await ctx.Interaction.RespondAsync(ex.Message);
            }
            finally
            {
                Log.Information("InteractionCreated End: {0}", interaction.Id);
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

        // Get information about a guild, when this completes it means the bot is in a sensible state to start other services.
        await Client.Rest.GetGuildAsync(537765528991825920);

        // Some extra delay for good measure. We really don't want to start other stuff while the bot isn't ready!
        await Task.Delay(1000);
    }

    private static async Task CommandExecuted(Optional<CommandInfo> command, ICommandContext context,  IResult result)
    {
        // Only pay attention to commands which fail due to an exception
        if (result.IsSuccess || result.Error is not CommandError.Exception)
            return;

        if (result is ExecuteResult er)
            Log.Error(er.Exception, "CommandExecuted completed with: {0}", er.ErrorReason);

        await context.Channel.SendMessageAsync("Command Exception! " + result.ErrorReason);
    }

    /// <summary>
    /// Stop the bot
    /// </summary>
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
                    Log.Error(ex, "Generic command preprocessor failed: {0}", pre.GetType().Name);
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
            Log.Error(ex, "Message handler threw exception");
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
            Log.Error(e, "Executing command threw an exception");
        }
    }

    /// <summary>
    /// Configure discord related services on the DI container
    /// </summary>
    /// <param name="services"></param>
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
            UseInteractionSnowflakeDate = false
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

    private static async Task LogAsync(LogMessage message)
    {
        var severity = message.Severity switch
        {
            LogSeverity.Critical => LogEventLevel.Fatal,
            LogSeverity.Error => LogEventLevel.Error,
            LogSeverity.Warning => LogEventLevel.Warning,
            LogSeverity.Info => LogEventLevel.Information,
            LogSeverity.Verbose => LogEventLevel.Verbose,
            LogSeverity.Debug => LogEventLevel.Debug,
            _ => LogEventLevel.Information
        };
        Log.Write(severity, message.Exception, "[{Source}] {Message}", message.Source, message.Message);
        await Task.CompletedTask;
    }
}