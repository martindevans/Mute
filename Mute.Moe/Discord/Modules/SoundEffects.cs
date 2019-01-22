using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using JetBrains.Annotations;
using MoreLinq;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Discord.Services.Audio;

namespace Mute.Moe.Discord.Modules
{
    [Group("sfx")]
    public class SoundEffects
        : BaseModule
    {
        private readonly SoundEffectService _sfx;
        private readonly IHttpClient _http;
        
        public SoundEffects([NotNull] SoundEffectService sfx, IHttpClient http)
        {
            _sfx = sfx;
            _http = http;
        }

        [Command, Summary("I will join the voice channel you are in, play a sound effect and leave"), Priority(0)]
        public async Task Play([NotNull] string id)
        {
            //Try to play a clip by the given ID
            var (ok, msg) = await _sfx.Play(Context.User, id);
            if (ok)
                return;

            //Type back the error encountered while playing this clip
            await TypingReplyAsync(msg);
        }

        [Command("find"), Summary("I will list all available sfx"), Priority(1)]
        public async Task Find([NotNull] string search)
        {
            var sfx = (await _sfx.Find(search)).OrderBy(a => a.Name).ToArray();

            if (sfx.Length == 0)
            {
                await TypingReplyAsync($"Search for `{search}` found no results");
            }
            else if (sfx.Length < 10)
            {
                await TypingReplyAsync(string.Join("\n", sfx.Select((a, i) => $"{i + 1}. `{a.Name}`")));
            }
            else
            {
                await PagedReplyAsync(new PaginatedMessage {
                    Pages = sfx.Select(a => a.Name).Batch(10).Select(b => string.Join("\n", b)).ToArray(),
                    Color = Color.Green,
                    Title = $"Sfx \"{search}\""
                });
            }
        }

        [Command("create"), Summary("I will add a new sound effect to the database"), Priority(1)]
        public async Task Create([NotNull] string name)
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

            //Create the sound effect
            var (ok, msg) = await _sfx.Create(name, data);
            if (ok)
            {
                await TypingReplyAsync($"Created new sound effect `{name}`!");
            }
            else
            {
                await TypingReplyAsync($"Failed to create new sound effect. {msg}");
            }
        }

        [Command("alias"), Summary("I will create an alias for another sound effect"), Priority(1)]
        public async Task Alias([NotNull] string name, [NotNull] string alias)
        {
            var a = await _sfx.Get(name);
            var b = await _sfx.Get(alias);

            if (a.HasValue && b.HasValue)
            {
                await TypingReplyAsync($"Cannot create an alias because `{name}` and `{alias}` both already exist");
                return;
            }

            if (!a.HasValue && !b.HasValue)
            {
                await TypingReplyAsync($"Cannot create an alias because neither `{name}` nor `{alias}` exist");
                return;
            }

            if (!a.HasValue)
            {
                await _sfx.Alias(b.Value, name);
                await TypingReplyAsync($"Aliased `{b.Value.Name}` as `{name}`");
            }
            else
            {
                await _sfx.Alias(a.Value, alias);
                await TypingReplyAsync($"Aliased `{a.Value.Name}` as `{alias}`");
            }
        }

        [Command("renormalize"), RequireOwner, Priority(1)]
        [ThinkingReply]
        public async Task Renormalize()
        {
            // Add a thinking emote while doing the work
            await _sfx.NormalizeAllSfx();
        }
    }
}
