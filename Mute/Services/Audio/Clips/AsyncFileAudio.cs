using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using NAudio.Wave;

namespace Mute.Services.Audio.Clips
{
    public class AsyncFileAudio
        : IAudioClip
    {
        public string Name { get; private set; }

        public AudioClipType Type { get; }

        private readonly Task _fileTask;
        private FileInfo _file;

        public AsyncFileAudio([NotNull] Task<FileInfo> file, AudioClipType type)
        {
            Type = type;
            Name = "Loading...";

            _fileTask = Task.Factory.StartNew(() => { _file = file.Result; });
        }

        public bool IsLoaded => _fileTask.IsCompleted && _file != null;

        [NotNull] public ISampleProvider Open()
        {
            Name = _file.Name;
            return new AudioFileReader(_file.FullName);
        }
    }
}
