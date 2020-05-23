using System.Collections.Generic;
using System.Threading.Tasks;


namespace Mute.Moe.Services.SoundEffects
{
    public interface ISoundEffectLibrary
    {
        /// <summary>
        /// Create a new sound effect
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="name"></param>
        /// <param name="audio"></param>
        /// <returns></returns>
        Task<ISoundEffect> Create(ulong guild,  string name,  byte[] audio);

        /// <summary>
        /// Alias an existing sound effect with a new name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        Task<ISoundEffect> Alias(string name, ISoundEffect other);

        /// <summary>
        /// Get sound effect with exact name
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        Task<ISoundEffect?> Get(ulong guild,  string name);

        /// <summary>
        /// Search for sound effects similar to the search term
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="search"></param>
        /// <returns></returns>
        IAsyncEnumerable<ISoundEffect> Find(ulong guild,  string search);
    }
}
