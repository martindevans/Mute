using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Discord.Services.Avatar;
using Mute.Moe.Discord.Services.ComponentActions;
using Mute.Moe.Discord.Services.Responses;
using Mute.Moe.Services.Information.Weather;

namespace Mute.Moe.Discord.Modules.Introspection;

[UsedImplicitly]
[RequireOwner]
public class Administration
    : BaseModule
{
    private readonly Configuration _config;
    private readonly DiscordSocketClient _client;
    private readonly ConversationalResponseService _conversations;
    private readonly IAvatarPicker _avatar;
    private readonly ComponentActionService _actions;
    private readonly IWeather _weather;

    public Administration(Configuration config, DiscordSocketClient client, ConversationalResponseService conversations, IAvatarPicker avatar, ComponentActionService actions, IWeather weather)
    {
        _config = config;
        _client = client;
        _conversations = conversations;
        _avatar = avatar;
        _actions = actions;
        _weather = weather;
    }

    [Command("say"), Summary("I will say whatever you want, but I won't be happy about it >:(")]
    [UsedImplicitly]
    public async Task Say(string message, IMessageChannel? channel = null)
    {
        channel ??= Context.Channel;

        await channel.TypingReplyAsync(message);
    }

    [Command("conversation-status"), Summary("I will show the status of my current conversation with a user")]
    [UsedImplicitly]
    public async Task ConversationState(IGuildUser? user = null)
    {
        user ??= Context.Message.Author as IGuildUser;

        if (user == null)
            await TypingReplyAsync("No user!");
        else
        {
            var c = _conversations.GetConversation(user);
            if (c == null)
                await TypingReplyAsync("No active conversation");
            else if (c.IsComplete)
                await TypingReplyAsync($"Conversation is complete `{c.GetType()}`");
            else
            {
                await TypingReplyAsync($"Conversation is active `{c.GetType()}`...");
                await ReplyAsync(c.ToString());
            }
        }
    }

    [Command("presence"), Summary("I will set my presence")]
    [UsedImplicitly]
    public Task SetPresence(ActivityType activity, [Remainder] string? presence)
    {
        if (string.IsNullOrEmpty(presence))
            return Task.CompletedTask;

        return activity == ActivityType.CustomStatus
            ? _client.SetActivityAsync(new CustomStatusGame(presence))
            : _client.SetActivityAsync(new Game(presence, activity));
    }

    [Command("status"), Summary("I will set my status")]
    [UsedImplicitly]
    public Task SetStatus(UserStatus status)
    {
        return _client.SetStatusAsync(status);
    }

    [Command("kill"), Alias("die", "self-destruct", "terminate"), Summary("I will immediately terminate my process ⊙︿⊙")]
    [UsedImplicitly]
    public async Task Kill(int exitCode = -1)
    {
        try
        {
            switch (DateTime.UtcNow.Millisecond % 10)
            {
                case 0:
                    await ReplyAsync($"Et tu, {Context.User.Username}?");
                    return;
                default:
                    await ReplyAsync("x_x");
                    break;
            }
        }
        finally
        {
            Environment.Exit(exitCode);
        }
    }

    [Command("nickname"), Alias("nick"), Summary("Set my nickname")]
    [UsedImplicitly]
    public Task Nickname([Remainder] string name)
    {
        return Context.Guild.CurrentUser.ModifyAsync(a => a.Nickname = name);
    }

    [Command("repick-avatar")]
    [UsedImplicitly]
    [ThinkingReply]
    public async Task RepickAvatar()
    {
        var result = await _avatar.PickAvatarNow();

        if (result.Choice == null)
            await Context.Channel.SendMessageAsync($"Failed to choose an avatar from `{result.Options.Count}` options");
        else
            await Context.Channel.SendMessageAsync($"Chose `{result.Choice}` from `{result.Options.Count}` options");
    }

    [Command("choose-avatar")]
    [UsedImplicitly]
    [ThinkingReply]
    public async Task ChooseAvatar(int choice = -1)
    {
        var options = await _avatar.GetOptions();

        if (choice > 0)
        {
            var result = await _avatar.SetAvatarNow(options[choice - 1]);

            if (result.Choice == null)
                await Context.Channel.SendMessageAsync($"Failed to choose an avatar from `{result.Options.Count}` options");
            else
                await Context.Channel.SendMessageAsync($"Chose `{result.Choice}` from `{result.Options.Count}` options");
        }
        else
        {
            await DisplayItemList(
                options,
                () => "No avatar options available",
                item => ReplyAsync(item),
                items => $"There are {items.Count} matching macros:",
                (item, idx) => $"{idx}. `{item}`"
            );
        }
    }

    [Command("DeleteThat")]
    [UsedImplicitly]
    public async Task DeleteThat(string id)
    {
        var msg = await Context.Channel.GetMessageAsync(ulong.Parse(id));
        await msg.DeleteAsync();
    }

    [Command("test_weather")]
    [UsedImplicitly]
    public async Task Weather()
    {
        // Use a random location in middle of UK if none is specified
        var pos = _config.Location ?? new LocationConfig
        {
            Latitude = 52.49f,
            Longitude = -1.23f
        };

        var report = await _weather.GetCurrentWeather(pos.Latitude, pos.Longitude);
        await ReplyAsync(report?.Description ?? "Unknown weather");
    }
}