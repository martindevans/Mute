using System;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Discord;
using JetBrains.Annotations;
using Mute.Moe.Discord.Services.Audio.Playback;
using NAudio.Wave;

namespace Mute.Moe.Services.SoundEffects
{
    //todo: this needs to be modified to support multiple guilds

    public interface ISoundEffectPlayer
    {
        Task<(PlayResult, Task)> Play([NotNull] IUser player, [NotNull] ISoundEffect sfx);
    }

    public enum PlayResult
    {
        Enqueued,
        UserNotInVoice,
        FileNotFound,
    }

    public class SoundEffectPlayer
        : ISoundEffectPlayer
    {
        private readonly SimpleQueueChannel<ISoundEffect> _queue = new SimpleQueueChannel<ISoundEffect>();
        private readonly MultichannelAudioService _audio;
        private readonly IFileSystem _fs;

        public SoundEffectPlayer([NotNull] MultichannelAudioService audio, [NotNull] IFileSystem fs)
        {
            _audio = audio;
            _fs = fs;

            _audio.Open(_queue);
        }

        public async Task<(PlayResult, Task)> Play(IUser user, ISoundEffect sfx)
        {
            if (!_fs.File.Exists(sfx.Path))
            {
                Console.WriteLine($"SFX not found: {sfx.Path}");
                return (PlayResult.FileNotFound, Task.CompletedTask);
            }

            if (!await _audio.MoveChannel(user))
                return (PlayResult.UserNotInVoice, Task.CompletedTask);

            var finishedTask = _queue.Enqueue(sfx, new AudioFileReader(sfx.Path));
            return (PlayResult.Enqueued, finishedTask);
        }
    }
}
