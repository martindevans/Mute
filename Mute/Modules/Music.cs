using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Audio;
using Discord.Commands;
using Mute.Extensions;
using Mute.Services;
using Mute.Services.Audio;
using NAudio.Wave;

namespace Mute.Modules
{
    public class Music
        : InteractiveBase
    {
        private readonly FileSystemService _fs;
        private readonly AudioPlayerService _audio;

        public Music(FileSystemService fs, AudioPlayerService audio)
        {
            _fs = fs;
            _audio = audio;
        }

        [Command("leave-voice")]
        [RequireOwner]
        public async Task Play2()
        {
            if (Context.User is IVoiceState v)
            {
                using (var d = await v.VoiceChannel.ConnectAsync())
                    await Task.Delay(100);
            }
            else
            {
                await ReplyAsync("You are not in a voice channel");
            }
        }

        [Command("skip")]
        [RequireOwner]
        public async Task Skip()
        {
            _audio.Skip();
        }

        [Command("play")]
        [RequireOwner]
        public async Task Play2(string name)
        {
            //Find the file and early exit if we cannot
            var f = new FileInfo(Path.Combine(@"C:\Users\Martin\Documents\dotnet\Mute\Test Music\", name));
            if (!f.Exists)
            {
                await this.TypingReplyAsync($"Cannot find file `{name}`");
                return;
            }

            if (Context.User is IVoiceState v)
            {
                _audio.Enqueue(new FileAudio(f, AudioClipType.Music));
                _audio.Channel = v.VoiceChannel;
                _audio.Play();
            }
            else
            {
                await ReplyAsync("You are not in a voice channel");
            }
        }
    }
}
