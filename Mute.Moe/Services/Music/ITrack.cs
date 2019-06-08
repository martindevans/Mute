using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Mute.Moe.Services.Music
{
    public interface ITrack
    {
        /// <summary>
        /// The guild which owns this track
        /// </summary>
        ulong Guild { get; }

        /// <summary>
        /// Unique ID for this track
        /// </summary>
        ulong ID { get; }

        /// <summary>
        /// Path to the file on disk with this audio
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
        /// Url of a thumbnail for this track
        /// </summary>
        [CanBeNull] string ThumbnailUrl { get; }

        /// <summary>
        /// Duration of this track
        /// </summary>
        TimeSpan Duration { get; }
    }
}
