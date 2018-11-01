using System;
using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using Mute.Services.Audio;
using System.Linq;
using Discord;
using JetBrains.Annotations;

namespace Mute.Modules
{
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

        [Command("sfx"), Summary("I will join the voice channel you are in, play a sound effect and leave")]
        public async Task Play(string id)
        {
            //Try to play a clip by the given ID
            var (ok, msg) = await _sfx.Play(Context.User, id);
            if (ok)
                return;

            //Type back the error encountered while playing this clip
            await TypingReplyAsync(msg);
        }

        [Command("sfx-find"), Summary("I will list all available sfx")]
        public async Task Find(string search)
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
                    Pages = sfx.Select(a => a.Name).ToArray(),
                    Color = Color.Green,
                    Title = $"Sfx \"{search}\""
                });
            }
        }

        [Command("sfx-create"), Summary("I will add a new sound effect to the database")]
        public async Task Create(string name)
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
    }
}
