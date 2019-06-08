using System;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Mute.Moe.Services.Music;
using NAudio.Wave;

namespace Mute.Moe.Services.Audio.Clips
{
    public class AsyncFileClip
        : IAudioClip
    {
        [CanBeNull] private readonly Func<Task<ITrack>> _track;
        public Task<ITrack> Track
        {
            get
            {
                if (_track == null)
                    return Task.FromResult<ITrack>(null);
                else
                    return _track();
            }
        }

        public string Name { get; }

        private readonly Task<FileInfo> _fileLoading;

        public AsyncFileClip([NotNull] Task<FileInfo> file, [NotNull] string name, [CanBeNull] Func<Task<ITrack>> track = null)
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
}
