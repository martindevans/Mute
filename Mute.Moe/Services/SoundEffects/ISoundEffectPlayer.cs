using System.IO.Abstractions;
using System.Threading.Tasks;
using Discord;
using JetBrains.Annotations;
using Mute.Moe.Services.Audio;
using NAudio.Wave;

namespace Mute.Moe.Services.SoundEffects
{
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
        private readonly IGuildVoiceCollection _guildAudio;
        private readonly IFileSystem _fs;

        public SoundEffectPlayer([NotNull] IGuildVoiceCollection guildAudio, [NotNull] IFileSystem fs)
        {
            _guildAudio = guildAudio;
            _fs = fs;
        }

        public async Task<(PlayResult, Task)> Play(IUser user, ISoundEffect sfx)
        {
            if (!_fs.File.Exists(sfx.Path))
                return (PlayResult.FileNotFound, Task.CompletedTask);

            if (!(user is IVoiceState vs))
                return (PlayResult.UserNotInVoice, Task.CompletedTask);

            var player = await _guildAudio.GetPlayer(vs.VoiceChannel.Guild);
            await player.Move(vs.VoiceChannel);

            var queue = player.Open<ISoundEffect>("sfx");

            var finishedTask = await queue.Enqueue(sfx, new AudioFileReader(sfx.Path));
            return (PlayResult.Enqueued, finishedTask);
        }
    }
}
