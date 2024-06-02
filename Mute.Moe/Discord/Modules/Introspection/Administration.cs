using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using JetBrains.Annotations;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Discord.Services.Avatar;
using Mute.Moe.Discord.Services.ComponentActions;
using Mute.Moe.Discord.Services.Responses;
using Mute.Moe.Extensions;

namespace Mute.Moe.Discord.Modules.Introspection;

[UsedImplicitly]
[RequireOwner]
public class Administration
    : BaseModule
{
    private readonly DiscordSocketClient _client;
    private readonly ConversationalResponseService _conversations;
    private readonly IAvatarPicker _avatar;
    private readonly ComponentActionService _actions;

    public Administration(DiscordSocketClient client, ConversationalResponseService conversations, IAvatarPicker avatar, ComponentActionService actions)
    {
        _client = client;
        _conversations = conversations;
        _avatar = avatar;
        _actions = actions;
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

        if (choice >= 0)
        {
            var result = await _avatar.SetAvatarNow(options[choice]);

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

    [Command("test-ui")]
    [UsedImplicitly]
    public async Task TestUI()
    {
        var builder = new ComponentBuilder();

        builder.AddRow(new ActionRowBuilder()
                      .WithButton("Primary", style: ButtonStyle.Primary, customId: "a")
                      .WithButton("Secondary", style: ButtonStyle.Secondary, customId: "b")
                      .WithButton("Danger", style: ButtonStyle.Danger, customId: "c")
        );

        builder.AddRow(new ActionRowBuilder()
                      .WithButton("Link", style: ButtonStyle.Link, url: "https://placeholder.software")
                      .WithButton("Success", style: ButtonStyle.Success, customId: "d")
        );

        builder.AddRow(new ActionRowBuilder()
           .WithSelectMenu("e", minValues: 1, maxValues: 3, options:
            [
                new SelectMenuOptionBuilder("Label A", "A"),
                new SelectMenuOptionBuilder("Label B", "B"),
                new SelectMenuOptionBuilder("Label C", "C"),
            ])
        );

        builder.AddRow(new ActionRowBuilder()
           .WithSelectMenu("f", minValues: 1, maxValues: 3, type: ComponentType.UserSelect)
        );

        var result = await ReplyWithActionsAsync(builder, "Here are some buttons!");
        await result.RespondAsync($"You clicked on: {result.Data.CustomId}");
    }

    private async Task<SocketMessageComponent> ReplyWithActionsAsync(ComponentBuilder builder, string? message = null)
    {
        // Get a waiter for every action in the builder
        var ids = builder.ActionRows.SelectMany(a => a.Components).Select(c => c.CustomId).Where(a => a != null).ToList();
        var tasks = ids.Select(_actions.GetWaiter).ToList();

        // Send the message
        var msg = await ReplyAsync(message, components: builder.Build());

        // Wait for something to come back
        var finished = await Task.WhenAny(tasks);

        // Destroy the original
        foreach (var id in ids)
            _actions.DestroyWaiter(id);
        await msg.DeleteAsync();

        return await finished;
    }
}