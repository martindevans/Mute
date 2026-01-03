using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Discord.Services.Avatar;
using Mute.Moe.Discord.Services.Responses;

namespace Mute.Moe.Discord.Modules.Introspection;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

[UsedImplicitly]
[RequireOwner]
public class Administration
    : MuteBaseModule
{
    private readonly DiscordSocketClient _client;
    private readonly IAvatarPicker _avatar;

    public Administration(DiscordSocketClient client, IAvatarPicker avatar)
    {
        _client = client;
        _avatar = avatar;
    }

    [Command("say"), Summary("I will say whatever you want, but I won't be happy about it >:(")]
    [UsedImplicitly]
    public async Task Say(string message, IMessageChannel? channel = null)
    {
        channel ??= Context.Channel;

        await channel.TypingReplyAsync(message);
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
}