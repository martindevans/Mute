using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Mute.Moe.Discord.Services.Audio.Clips
{
    public class YoutubeAsyncFileAudio
        : AsyncFileAudio
    {
        public string ID { get; }

        public YoutubeAsyncFileAudio(string id, [NotNull] Task<FileInfo> file)
            : base(file)
        {
            ID = id;
        }
    }
}
