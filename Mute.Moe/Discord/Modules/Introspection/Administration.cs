using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using JetBrains.Annotations;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Discord.Services.Avatar;
using Mute.Moe.Discord.Services.Responses;
using Mute.Moe.Discord.Services.Responses.Enigma;
using Mute.Moe.Extensions;
using Mute.Moe.Services.Audio.Sources.Youtube;

namespace Mute.Moe.Discord.Modules.Introspection;

[UsedImplicitly]
[RequireOwner]
public class Administration
    : BaseModule
{
    private readonly DiscordSocketClient _client;
    private readonly ConversationalResponseService _conversations;
    private readonly IYoutubeDownloader _yt;
    private readonly SeasonalAvatar _avatar;
    private readonly EnigmaResponse _enigma;

    public Administration(DiscordSocketClient client, ConversationalResponseService conversations, IYoutubeDownloader yt, SeasonalAvatar avatar, EnigmaResponse enigma)
    {
        _client = client;
        _conversations = conversations;
        _yt = yt;
        _avatar = avatar;
        _enigma = enigma;
    }

    [Command("say"), Summary("I will say whatever you want, but I won't be happy about it >:(")]
    public async Task Say(string message, IMessageChannel? channel = null)
    {
        channel ??= Context.Channel;

        await channel.TypingReplyAsync(message);
    }

    [Command("conversation-status"), Summary("I will show the status of my current conversation with a user")]
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

    [Command("enigma-status"), Summary("I will show the status of my current enigma conversation")]
    public async Task EnigmaState(IChannel? channel = null)
    {
        await TypingReplyAsync($"There are {_enigma.Count} total active conversations");

        channel ??= Context.Message.Channel;
        var state = _enigma.GetState(channel);
        if (state == null)
        {
            await TypingReplyAsync($"No active conversation state for {channel.Name}");
            return;
        }

        await ReplyAsync($"{channel.Name}: {state}");
    }

    [Command("presence"), Summary("I will set my presence")]
    public async Task SetPresence(ActivityType activity, [Remainder] string? presence)
    {
        if (!string.IsNullOrEmpty(presence))
            await _client.SetActivityAsync(new Game(presence, activity));
    }

    [Command("status"), Summary("I will set my status")]
    public async Task SetPresence(UserStatus status)
    {
        await _client.SetStatusAsync(status);
    }

    [Command("kill"), Alias("die", "self-destruct", "terminate"), Summary("I will immediately terminate my process ⊙︿⊙")]
    public Task Kill(int exitCode = -1)
    {
        Environment.Exit(exitCode);

        return Task.CompletedTask;
    }

    [Command("nickname"), Alias("nick"), Summary("Set my nickname")]
    public async Task Nickname([Remainder] string name)
    {
        await Context.Guild.CurrentUser.ModifyAsync(a => a.Nickname = name);
    }

    [Command("test-yt")]
    [ThinkingReply]
    public async Task TestYt(string url)
    {
        var result = await _yt.DownloadAudio(url);

        await ReplyAsync(result.Status.ToString());

        if (result is { Status: YoutubeDownloadStatus.Success, File: { } })
        {
            await ReplyAsync(result.File.File.FullName);
            await ReplyAsync(result.File.ThumbnailUrl);
            await ReplyAsync(result.File.Title);
            await ReplyAsync(result.File.Url);
        }

        result.File?.Dispose();
    }

    [Command("repick-avatar")]
    [ThinkingReply]
    public async Task RepickAvatar()
    {
        var result = await _avatar.PickDaily();

        if (result.Choice == null)
            await Context.Channel.SendMessageAsync($"Failed to choose an avatar from `{result.Options.Count}` options");
        else
            await Context.Channel.SendMessageAsync($"Chose `{result.Choice}` from `{result.Options.Count}` options");
    }
}