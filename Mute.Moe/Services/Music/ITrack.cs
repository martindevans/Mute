using System;
using JetBrains.Annotations;

namespace Mute.Moe.Services.Music
{
    public interface ITrack
    {
        /// <summary>
        /// Unique ID for this track
        /// </summary>
        ulong ID { get; }

        /// <summary>
        /// Path to the file which contains this audio track
        /// </summary>
        [NotNull] string Path { get; }

        /// <summary>
        /// Human readable title of this track
        /// </summary>
        [NotNull] string Title { get; }

        /// <summary>
        /// Url related to this track
        /// </summary>
        [CanBeNull] string Url { get; }

        /// <summary>
        /// The artist who composed this track
        /// </summary>
        [CanBeNull] IArtist Artist { get; }

        /// <summary>
        /// Duration of this track
        /// </summary>
        TimeSpan Duration { get; }
    }
}
