using System.IO.Abstractions;
using System.Threading.Tasks;
using Discord;

using NAudio.Wave;

namespace Mute.Moe.Services.SoundEffects;

public interface ISoundEffectPlayer
{
    Task<(PlayResult, Task)> Play( IUser player,  ISoundEffect sfx);
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
    private readonly IGuildSoundEffectQueueCollection _queueCollection;
    private readonly IFileSystem _fs;

    public SoundEffectPlayer( IGuildSoundEffectQueueCollection queueCollection,  IFileSystem fs)
    {
        _queueCollection = queueCollection;
        _fs = fs;
    }

    public async Task<(PlayResult, Task)> Play(IUser user, ISoundEffect sfx)
    {
        if (!_fs.File.Exists(sfx.Path))
            return (PlayResult.FileNotFound, Task.CompletedTask);

        if (user is not IVoiceState vs)
            return (PlayResult.UserNotInVoice, Task.CompletedTask);

        var q = await _queueCollection.Get(vs.VoiceChannel.Guild.Id);
        await q.VoicePlayer.Move(vs.VoiceChannel);

        var finishedTask = await q.Enqueue(sfx, new AudioFileReader(sfx.Path));
        return (PlayResult.Enqueued, finishedTask);
    }
}