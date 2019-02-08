using System;
using System.IO;
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
        Task<PlayResult> Play([NotNull] IUser player, [NotNull] ISoundEffect sfx);
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
        private readonly SoundEffectConfig _config;
        private readonly MultichannelAudioService _audio;
        private readonly IFileSystem _fs;

        public SoundEffectPlayer([NotNull] Configuration config, [NotNull] MultichannelAudioService audio, [NotNull] IFileSystem fs)
        {
            _config = config.SoundEffects;
            _audio = audio;
            _fs = fs;

            _audio.Open(_queue);
        }

        public async Task<PlayResult> Play(IUser user, ISoundEffect sfx)
        {
            if (!await _audio.MoveChannel(user))
                return PlayResult.UserNotInVoice;

            if (!_fs.File.Exists(sfx.Path))
            {
                Console.WriteLine($"SFX not found: {sfx.Path}");
                return PlayResult.FileNotFound;
            }

#pragma warning disable 4014 (this task completes when the sfx _finishes_)
            _queue.Enqueue(sfx, new AudioFileReader(sfx.Path));
#pragma warning restore 4014
            return PlayResult.Enqueued;
        }
    }
}
