using System;
using System.Threading.Tasks;
using Discord;
using JetBrains.Annotations;
using Mute.Moe.Services.Audio.Mixing.Channels;

namespace Mute.Moe.Services.Audio
{
    /// <summary>
    /// Sends/receives audio in a discord voice channel in a particular guild
    /// </summary>
    public interface IGuildVoice
    {
        [NotNull] IGuild Guild { get; }

        [CanBeNull] IVoiceChannel Channel { get; }

        Task Move([CanBeNull] IVoiceChannel channel);

        Task Stop();

        [NotNull] SimpleQueueChannel<T> Open<T>(string name);
    }
}
