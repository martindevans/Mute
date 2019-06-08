using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Mute.Moe.Services.Music
{
    public interface IMusicLibrary
    {
        /// <summary>
        /// Add a new track to the music library
        /// </summary>
        /// <param name="guild">Guild which owns this track</param>
        /// <param name="owner">Person who owns this track</param>
        /// <param name="audio">Open stream to read audio data from</param>
        /// <param name="title">Human readable title of this track</param>
        /// <param name="duration"></param>
        /// <param name="url">URL related to this track</param>
        /// <param name="thumbnailUrl"></param>
        /// <returns></returns>
        [NotNull, ItemNotNull] Task<ITrack> Add(ulong guild, ulong owner, [NotNull] Stream audio, [NotNull] string title, TimeSpan duration, [CanBeNull] string url = null, [CanBeNull] string thumbnailUrl = null);

        /// <summary>
        /// Search for tracks in the music library
        /// </summary>
        /// <param name="guild"></param>
        /// <param name="id"></param>
        /// <param name="titleSearch"></param>
        /// <param name="url"></param>
        /// <param name="limit"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        [NotNull, ItemNotNull] Task<IAsyncEnumerable<ITrack>> Get(ulong guild, ulong? id = null, [CanBeNull] string titleSearch = null, [CanBeNull] string url = null, int? limit = null, TrackOrder? order = null);
    }

    public enum TrackOrder
    {
        Random,
        Id
    }
}
