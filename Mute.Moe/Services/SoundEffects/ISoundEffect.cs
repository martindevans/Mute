namespace Mute.Moe.Services.SoundEffects
{
    public interface ISoundEffect
    {
        /// <summary>
        /// Path of the file which contains this sound effect
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Human readable name of this sound effect
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The guild which owns this sound effect
        /// </summary>
        ulong Guild { get; }
    }
}
