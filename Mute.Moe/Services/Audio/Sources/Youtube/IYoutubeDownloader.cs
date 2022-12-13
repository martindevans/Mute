using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;


namespace Mute.Moe.Services.Audio.Sources.Youtube;

public interface IYoutubeDownloader
{
    Task<bool> IsValidUrl(string url);

    Task<IYoutubeDownloadResult> DownloadAudio(string url);

    Task<int> PerformMaintenance();
}

public interface IYoutubeDownloadResult
{
    IYoutubeFile? File { get; }

    YoutubeDownloadStatus Status { get; }
}

/// <summary>
/// A youtube file on disk. Dispose this object to delete the file.
/// </summary>
public interface IYoutubeFile
    : IDisposable
{
    FileInfo File { get; }

    string Title { get; }

    string Url { get; }

    string? ThumbnailUrl { get; }

    IReadOnlyList<string> Artists { get; }

    TimeSpan Duration { get; }
}

public enum YoutubeDownloadStatus
{
    Success,

    FailedInvalidUrl,
    FailedDuringDownload,
    FailedInvalidDownloadResult
}