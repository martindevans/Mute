using System.Threading.Tasks;
using Discord;
using Mute.Moe.Services.Audio.Mixing.Channels;

namespace Mute.Moe.Services.Audio;

/// <summary>
/// Sends/receives audio in a discord voice channel in a particular guild
/// </summary>
public interface IGuildVoice
{
    /// <summary>
    /// Get the guild this is for
    /// </summary>
    IGuild Guild { get; }

    /// <summary>
    /// Get the voice channel the bot is currently in (if any)
    /// </summary>
    IVoiceChannel? Channel { get; }

    /// <summary>
    /// Try to move to a new voice channel
    /// </summary>
    /// <param name="channel"></param>
    /// <returns></returns>
    Task Move(IVoiceChannel? channel);

    /// <summary>
    /// Disconnect from voice
    /// </summary>
    /// <returns></returns>
    Task Stop();

    /// <summary>
    /// Add a new mixer channel to playback mixer
    /// </summary>
    /// <param name="channel"></param>
    void Open(IMixerChannel channel);
}