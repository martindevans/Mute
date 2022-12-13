using System;

namespace Mute.Moe.Services.Music;

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
    string Path { get; }

    /// <summary>
    /// Human readable title of this track
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Url related to this track
    /// </summary>
    string? Url { get; }

    /// <summary>
    /// Url of a thumbnail for this track
    /// </summary>
    string? ThumbnailUrl { get; }

    /// <summary>
    /// Duration of this track
    /// </summary>
    TimeSpan Duration { get; }
}