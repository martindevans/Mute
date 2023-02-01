using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using JetBrains.Annotations;
using MoreLinq;
using Mute.Moe.Extensions;
using Mute.Moe.Services.SoundEffects;
using Mute.Moe.Utilities;

namespace Mute.Moe.Discord.Modules.Audio;

[UsedImplicitly]
[Group("sfx")]
[RequireContext(ContextType.Guild)]
public class SoundEffects
    : BaseModule
{
    private readonly ISoundEffectLibrary _library;
    private readonly ISoundEffectPlayer _player;
    private readonly HttpClient _http;
    private readonly Random _random;

    public SoundEffects(ISoundEffectLibrary library, ISoundEffectPlayer player, IHttpClientFactory http, Random random)
    {
        _library = library;
        _player = player;
        _http = http.CreateClient();
        _random = random;
    }

    [Command, Summary("I will join the voice channel you are in and play a sound effect"), Priority(0)]
    [UsedImplicitly]
    public async Task Play( string id)
    {
        var found = await _library.Find(Context.Guild.Id, id).ToArrayAsync();

        if (found.Length == 0)
        {
            await TypingReplyAsync($"Cannot find any sound effects by search string `{id}`");
            return;
        }

        var sfx = found.Random(_random);

        var (result, finished) = await _player.Play(Context.User, sfx);
        switch (result)
        {
            case PlayResult.Enqueued:
                await Context.Message.AddReactionAsync(new Emoji(EmojiLookup.SpeakerLowVolume));
                await finished;
                await Context.Message.RemoveReactionAsync(new Emoji(EmojiLookup.SpeakerLowVolume), Context.Client.CurrentUser);
                return;
            case PlayResult.UserNotInVoice:
                await TypingReplyAsync("You are not in a voice channel!");
                return;
            case PlayResult.FileNotFound:
                await TypingReplyAsync("File not found.");
                return;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    [Command("find"), Summary("I will list all available sfx"), Priority(1)]
    [UsedImplicitly]
    public async Task Find( string search)
    {
        var sfx = await _library.Find(Context.Guild.Id, search).OrderBy(a => a.Name).ToArrayAsync();

        switch (sfx.Length)
        {
            case 0:
                await TypingReplyAsync($"Search for `{search}` found no results");
                break;

            case < 10:
                await TypingReplyAsync(string.Join("\n", sfx.Select((a, i) => $"{i + 1}. `{a.Name}`")));
                break;

            default:
                await PagedReplyAsync(new PaginatedMessage {
                    Pages = sfx.Select(a => a.Name).Batch(10).Select(b => string.Join("\n", b)).ToArray(),
                    Color = Color.Green,
                    Title = $"Sfx \"{search}\"",
                });
                break;
        }
    }

    [Command("create"), Summary("I will add a new sound effect to the database"), Priority(1)]
    [UsedImplicitly]
    public async Task Create( string name)
    {
        await TypingReplyAsync("Please upload an audio file for this sound effect. It must be under 15s and 1MiB!");

        //Wait for a reply with an attachment
        var reply = await NextMessageAsync(true, true, TimeSpan.FromSeconds(30));
        if (reply == null)
        {
            await TypingReplyAsync($"{Context.User.Mention} you didn't upload a file in time.");
            return;
        }

        //Sanity check that there is exactly one attachment
        if (!reply.Attachments.Any())
        {
            await TypingReplyAsync($"There don't seem to be any attachments to that message. I can't create sound effect `{name}` from that");
            return;
        }

        if (reply.Attachments.Count > 1)
        {
            await TypingReplyAsync($"There is more than one attachment to that message. I don't know which one to use to create sound effect `{name}`");
            return;
        }

        //Get attachment and sanity check size
        var attachment = reply.Attachments.Single();
        if (attachment.Size > 1048576)
        {
            await TypingReplyAsync($"The attachment is too large! I can't use that for sound effect `{name}`");
            return;
        }

        //Download a local copy of the attachment
        byte[] data;
        using (var download = await _http.GetAsync(attachment.ProxyUrl))
        {
            if (!download.IsSuccessStatusCode)
            {
                await TypingReplyAsync($"Downloading the attachment failed (Status:{download.StatusCode}): `{download.ReasonPhrase}`");
                return;
            }

            data = await download.Content.ReadAsByteArrayAsync();
        }

        if (await _library.Get(Context.Guild.Id, name) != null)
        {
            await TypingReplyAsync("A sound effect with this name already exists!");
            return;
        }

        //Create the sound effect
        await _library.Create(Context.Guild.Id, name, data);
        await TypingReplyAsync($"Created new sound effect `{name}`!");
    }

    [Command("alias"), Summary("I will create an alias for another sound effect"), Priority(1)]
    [UsedImplicitly]
    public async Task Alias( string name,  string alias)
    {
        var a = await _library.Get(Context.Guild.Id, name);
        var b = await _library.Get(Context.Guild.Id, alias);

        if (a != null && b != null)
        {
            await TypingReplyAsync($"Cannot create an alias because `{name}` and `{alias}` both already exist");
            return;
        }

        switch (a, b)
        {
            case (null, null):
                await TypingReplyAsync($"Cannot create an alias because neither `{name}` nor `{alias}` exist");
                return;

            case (null, _):
                await _library.Alias(name, b);
                await TypingReplyAsync($"Aliased `{b.Name}` as `{name}`");
                return;

            case (_, null):
                await _library.Alias(name, a);
                await TypingReplyAsync($"Aliased `{a.Name}` as `{alias}`");
                return;
        }
    }
}