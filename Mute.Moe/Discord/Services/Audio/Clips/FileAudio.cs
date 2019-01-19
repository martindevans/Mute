using System.IO;
using JetBrains.Annotations;
using NAudio.Wave;

namespace Mute.Moe.Discord.Services.Audio.Clips
{
    public class FileAudio
        : IAudioClip
    {
        private readonly FileInfo _file;

        public FileAudio([NotNull] FileInfo file)
        {
            _file = file;

            Name = file.Name;
        }

        public string Name { get; }

        public bool IsLoaded => true;

        [NotNull] public ISampleProvider Open()
        {
            return new AudioFileReader(_file.FullName);
        }
    }
}
