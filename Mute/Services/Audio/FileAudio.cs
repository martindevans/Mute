using System.IO;
using JetBrains.Annotations;
using NAudio.Wave;

namespace Mute.Services.Audio
{
    public class FileAudio
        : IAudioClip
    {
        private readonly FileInfo _file;

        public FileAudio([NotNull] FileInfo file, AudioClipType type)
        {
            _file = file;

            Type = type;
            Name = file.Name;
        }

        public string Name { get; }

        public AudioClipType Type { get; }

        [NotNull] public ISampleProvider Open()
        {
            return new AudioFileReader(_file.FullName);
        }
    }
}
