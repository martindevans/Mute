using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
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

        [Command("skip")]
        [RequireOwner]
        public Task Skip()
        {
            _audio.Skip();

            return Task.CompletedTask;
        }

        private async Task EnqueueMusicClip(Func<IAudioClip> clip)
        {
            if (Context.User is IVoiceState v)
            {
                try
                {
                    _audio.Enqueue(clip());
                    _audio.Channel = v.VoiceChannel;
                    _audio.Play();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            else
            {
                await ReplyAsync("You are not in a voice channel");
            }
        }

        [Command("play"), Summary("I will download and play audio from a youtube video into whichever voice channel you are in")]
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

            //Start downloading the video
            var download = Task.Factory.StartNew(async () => {
                var yt = await _youtube.GetYoutubeAudio(url);
                Console.WriteLine("Download complete");
                return yt;
            }).Unwrap();

            //Add the reaction options (from love to hate)
            await Context.Message.AddReactionAsync(EmojiLookup.Heart);
            await Context.Message.AddReactionAsync(EmojiLookup.ThumbsUp);
            await Context.Message.AddReactionAsync(EmojiLookup.Expressionless);
            await Context.Message.AddReactionAsync(EmojiLookup.ThumbsDown);
            await Context.Message.AddReactionAsync(EmojiLookup.BrokenHeart);

            //Finally enqueue the track
            await EnqueueMusicClip(() => new AsyncFileAudio(download, AudioClipType.Music));
        }
    }
}
