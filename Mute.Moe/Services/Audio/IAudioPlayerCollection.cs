using System.Threading.Tasks;
using Discord;

namespace Mute.Moe.Services.Audio
{
    /// <summary>
    /// Collection of audio players for guilds
    /// </summary>
    public interface IAudioPlayerCollection
    {
        /// <summary>
        /// Get the player for the given guild
        /// </summary>
        /// <param name="guild"></param>
        /// <returns></returns>
        Task<IMultichannelAudioPlayer> GetPlayer(IGuild guild);
    }
}
