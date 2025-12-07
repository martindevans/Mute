using System.IO;
using System.Threading.Tasks;
using NAudio.Wave;

namespace Mute.Moe.Services.Audio.Clips;

/// <summary>
/// An audio clip which loads from a file
/// </summary>
public class AsyncFileClip
    : IAudioClip
{
    /// <inheritdoc />
    public string Name { get; }

    private readonly Task<FileInfo> _fileLoading;

    public AsyncFileClip(Task<FileInfo> file, string name)
    {
        _fileLoading = file;

        Name = name;
    }

    /// <inheritdoc />
    public async Task<IOpenAudioClip> Open()
    {
        var f = await _fileLoading;
        return new OpenAudioClipSamplesWrapper<AudioFileReader>(this, new AudioFileReader(f.FullName));
    }
}