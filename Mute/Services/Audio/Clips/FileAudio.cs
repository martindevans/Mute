using System.IO;
using JetBrains.Annotations;
using NAudio.Wave;

namespace Mute.Services.Audio.Clips
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

        public bool IsLoaded => true;

        [NotNull] public ISampleProvider Open()
        {
            return new AudioFileReader(_file.FullName);
        }
    }
}
