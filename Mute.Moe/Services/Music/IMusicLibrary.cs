using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Mute.Moe.Services.Music
{
    public interface IMusicLibrary
    {
        /// <summary>
        /// Add a new track to the music library
        /// </summary>
        /// <param name="path"></param>
        /// <param name="title"></param>
        /// <param name="url"></param>
        /// <param name="artist"></param>
        /// <returns></returns>
        Task<ITrack> Add([NotNull] string path, [NotNull] string title, [CanBeNull] string url = null, [CanBeNull] string artist = null);

        /// <summary>
        /// Search for tracks in the music library
        /// </summary>
        /// <param name="id"></param>
        /// <param name="minRating"></param>
        /// <param name="maxRating"></param>
        /// <param name="minPlayCount"></param>
        /// <param name="maxPlayCount"></param>
        /// <param name="artistName"></param>
        /// <param name="artist"></param>
        /// <returns></returns>
        Task<IAsyncEnumerable<ITrack>> Get(ulong? id = null, byte? minRating = null, byte? maxRating = null, uint? minPlayCount = null, uint? maxPlayCount = null, string artistName = null, ulong? artist = null);

        /// <summary>
        /// Increment the play count for a track
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task Played(ulong id);

        /// <summary>
        /// Record the track rating for a given user and track
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="trackId"></param>
        /// <param name="rating"></param>
        /// <returns></returns>
        Task Rating(ulong userId, ulong trackId, byte rating);
    }
}
