using System.Threading.Tasks;


namespace Mute.Moe.Services.Audio
{
    /// <summary>
    /// Collection of audio players for guilds
    /// </summary>
    public interface IGuildVoiceCollection
    {
        /// <summary>
        /// Get the player for the given guild
        /// </summary>
        /// <param name="guild"></param>
        /// <returns></returns>
        Task<IGuildVoice> GetPlayer(ulong guild);
    }
}
