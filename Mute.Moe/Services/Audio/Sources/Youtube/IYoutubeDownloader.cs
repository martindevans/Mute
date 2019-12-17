using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Mute.Moe.Services.Audio.Sources.Youtube
{
    public interface IYoutubeDownloader
    {
        [NotNull] Task<bool> IsValidUrl([NotNull] string url);

        [NotNull, ItemNotNull] Task<IYoutubeDownloadResult> DownloadAudio([NotNull] string url);

        [NotNull] Task<int> PerformMaintenance();
    }

    public interface IYoutubeDownloadResult
    {
        [CanBeNull] IYoutubeFile File { get; }

        YoutubeDownloadStatus Status { get; }
    }

    /// <summary>
    /// A youtube file on disk. Dispose this object to delete the file.
    /// </summary>
    public interface IYoutubeFile
        : IDisposable
    {
        [NotNull] FileInfo File { get; }

        [NotNull] string Title { get; }

        [NotNull] string Url { get; }

        [CanBeNull] string ThumbnailUrl { get; }

        [NotNull] IReadOnlyList<string> Artists { get; }

        TimeSpan Duration { get; }
    }

    public enum YoutubeDownloadStatus
    {
        Success,

        FailedInvalidUrl,
        FailedDuringDownload,
        FailedInvalidDownloadResult
    }
}
