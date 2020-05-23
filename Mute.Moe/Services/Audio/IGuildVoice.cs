using System.Threading.Tasks;
using Discord;

using Mute.Moe.Services.Audio.Mixing.Channels;

namespace Mute.Moe.Services.Audio
{
    /// <summary>
    /// Sends/receives audio in a discord voice channel in a particular guild
    /// </summary>
    public interface IGuildVoice
    {
         IGuild Guild { get; }

        IVoiceChannel? Channel { get; }

        Task Move(IVoiceChannel? channel);

        Task Stop();

        void Open(IMixerChannel channel);
    }
}
