using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Mute.Services.Audio.Clips
{
    class YoutubeAsyncFileAudio
        : AsyncFileAudio
    {
        public string ID { get; }

        public YoutubeAsyncFileAudio(string id, [NotNull] Task<FileInfo> file, AudioClipType type)
            : base(file, type)
        {
            ID = id;
        }
    }
}
