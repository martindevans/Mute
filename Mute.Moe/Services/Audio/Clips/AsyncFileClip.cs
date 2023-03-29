using System;
using System.IO;
using System.Threading.Tasks;
using Mute.Moe.Services.Music;
using NAudio.Wave;

namespace Mute.Moe.Services.Audio.Clips;

public class AsyncFileClip
    : IAudioClip
{
    private readonly Func<Task<ITrack?>>? _track;
    public Task<ITrack?> Track => _track == null ? Task.FromResult<ITrack?>(null) : _track();

    public string Name { get; }

    private readonly Task<FileInfo> _fileLoading;

    public AsyncFileClip( Task<FileInfo> file, string name, Func<Task<ITrack?>>? track = null)
    {
        _fileLoading = file;
        _track = track;

        Name = name;
    }

    public async Task<IOpenAudioClip> Open()
    {
        var f = await _fileLoading;
        return new OpenAudioClipSamplesWrapper<AudioFileReader>(this, new AudioFileReader(f.FullName));
    }
}