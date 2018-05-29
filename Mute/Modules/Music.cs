using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using JetBrains.Annotations;
using Mute.Extensions;
using Mute.Services;
using Mute.Services.Audio;
using Mute.Services.Audio.Clips;

namespace Mute.Modules
{
    [Group]
    [RequireOwner]
    public class Music
        : InteractiveBase
    {
        private readonly FileSystemService _fs;
        private readonly AudioPlayerService _audio;
        private readonly YoutubeService _youtube;

        public Music(FileSystemService fs, AudioPlayerService audio, YoutubeService youtube)
        {
            _fs = fs;
            _audio = audio;
            _youtube = youtube;
        }

        [Command("leave-voice")]
        [RequireOwner]
        public async Task LeaveVoice()
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

        [Command("download_youtube")]
        [RequireOwner]
        public async Task DownloadYoutubeUrl([NotNull] string urlString)
        {
            //Sanity check that it's even a well formed URL
            if (!Uri.TryCreate(urlString, UriKind.Absolute, out var url))
            {
                await this.TypingReplyAsync("That's not a valid URL");
                return;
            }

            //Sanity check that the scheme is correct
            if (url.Scheme != "http" && url.Scheme != "https")
            {
                await this.TypingReplyAsync("URL scheme must be `http` or `https`");
                return;
            }

            //Get the file with the content of this video
            try
            {
                await this.TypingReplyAsync("Starting");
                var r = await _youtube.GetYoutubeAudio(urlString);
                await this.TypingReplyAsync("Downloaded " + r.FullName + " " + r.Exists);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            //https://www.youtube.com/watch?v=i7Gkh_9hyi8
        }

        [Command("skip")]
        [RequireOwner]
        public Task Skip()
        {
            _audio.Skip();

            return Task.CompletedTask;
        }

        [Command("play_youtube")]
        [RequireOwner]
        public async Task EnqueueYoutube(string url)
        {
            //Check that the URL is valid
            try
            {
                _youtube.CheckUrl(url);
            }
            catch (Exception e)
            {
                await this.TypingReplyAsync(e.Message);
                return;
            }

            //Check that the user is in a voice channel
            if (Context.User is IVoiceState v)
            {
                //Start downloading the video
                var download = Task.Factory.StartNew(async () => {
                    var yt = await _youtube.GetYoutubeAudio(url);
                    Console.WriteLine("Download complete");
                    return yt;
                }).Unwrap();

                //Queue up the file to play
                _audio.Enqueue(new AsyncFileAudio(download, AudioClipType.Music));

                if (_audio.Channel != v.VoiceChannel)
                    _audio.Channel = v.VoiceChannel;
            }
            else
            {
                await ReplyAsync("You are not in a voice channel");
            }
        }

        [Command("play_file")]
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
