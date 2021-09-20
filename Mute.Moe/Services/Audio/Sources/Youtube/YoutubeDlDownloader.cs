using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Mute.Moe.Utilities;
using Newtonsoft.Json.Linq;

namespace Mute.Moe.Services.Audio.Sources.Youtube
{
    public class YoutubeDlDownloader
        : IYoutubeDownloader
    {
        private readonly string _rateLimit;
        private readonly string _inProgressDownloadFolder;
        private readonly string _ffmpegBinaryPath;
        private readonly string _youtubeDlBinaryPath;

        private readonly AsyncLock _mutex = new();

        public YoutubeDlDownloader(Configuration config)
        {
            if (config.YoutubeDl == null)
                throw new ArgumentNullException(nameof(config.YoutubeDl));

            _rateLimit = config.YoutubeDl.RateLimit ?? "1.5M";
            _inProgressDownloadFolder = config.YoutubeDl.InProgressDownloadFolder ?? throw new ArgumentNullException(nameof(config.YoutubeDl.InProgressDownloadFolder));
            _ffmpegBinaryPath = config.YoutubeDl.FfmpegBinaryPath ?? throw new ArgumentNullException(nameof(config.YoutubeDl.FfmpegBinaryPath));
            _youtubeDlBinaryPath = config.YoutubeDl.YoutubeDlBinaryPath ?? throw new ArgumentNullException(nameof(config.YoutubeDl.YoutubeDlBinaryPath));
        }

        private static bool IsValidUrl(string? urlString, out Uri? url)
        {
            if (urlString == null)
            {
                url = null;
                return false;
            }

            //Sanity check that URL is a well formed URL
            if (!Uri.TryCreate(urlString, UriKind.Absolute, out url))
                return false;

            // Check that it actually points at youtube
            if (url.Host != "www.youtube.com")
                return false;

            // Is it using a sensible scheme?
            if (url.Scheme != "http" && url.Scheme != "https")
                return false;

            // Check for any escape characters
            if (urlString.Contains("\"") || urlString.Contains("\'") || urlString.Contains("\\"))
                return false;

            // Check that it contains a video parameter
            if (HttpUtility.ParseQueryString(url.Query)["v"] == null)
                return false;

            return true;
        }

        public Task<bool> IsValidUrl(string urlString)
        {
            return Task.FromResult(IsValidUrl(urlString, out _));
        }

        public async Task<IYoutubeDownloadResult> DownloadAudio(string urlString)
        {
            if (!IsValidUrl(urlString, out var url) || url == null)
                return new FailedDownloadResult(YoutubeDownloadStatus.FailedInvalidUrl);

            //Choose a name for temp data, wrap in a try/finally block to ensure temp data is deleted no matter what happens
            var tempDownFile = Guid.NewGuid().ToString();
            try
            {
                var downloadingLocation = Path.GetFullPath(Path.Combine(_inProgressDownloadFolder, tempDownFile));
                var args = $"\"{url}\"" +
                           $" --no-check-certificate" +
                           $" --output \"{downloadingLocation}.%(ext)s\"" +
                           $" --quiet" +
                           $" --no-warnings" +
                           $" --no-playlist" +
                           $" --write-info-json" +
                           $" --limit-rate {_rateLimit}" +
                           $" --extract-audio" +
                           $" --audio-format wav" +
                           $" --ffmpeg-location \"{_ffmpegBinaryPath}\"" +
                           $" --geo-bypass" +
                           $" --no-call-home";

                //Download to the in-progress-download folder, early out if anything goes wrong
                try
                {
                    //Lock on the mutex to ensure only one download is in flight at a time
                    using (await _mutex.LockAsync())
                    {
                        await AsyncProcess.StartProcess(
                            Path.GetFullPath(_youtubeDlBinaryPath),
                            args,
                            Path.GetFullPath(_inProgressDownloadFolder)
                        );
                    }
                }
                catch (Exception)
                {
                    return new FailedDownloadResult(YoutubeDownloadStatus.FailedDuringDownload);
                }

                //Find the completed download. It should be two files
                // <guid>.wav contains the audio
                // <guid>.json contains the metadata (including the video ID)
                var maybeCompleteFiles = Directory.GetFiles(Path.GetFullPath(_inProgressDownloadFolder), tempDownFile + ".*").Select(f => new FileInfo(f)).ToArray();

                //Find the two files we want
                var audioFile = maybeCompleteFiles.SingleOrDefault(f => f.Extension == ".wav");
                var metadataFile = maybeCompleteFiles.SingleOrDefault(f => f.Extension == ".json");

                //if one is null something went wrong, delete everything anbd early exit
                if (audioFile == null || metadataFile == null)
                    return new FailedDownloadResult(YoutubeDownloadStatus.FailedDuringDownload);

                //Find out the video ID from the metadata
                var metadata = JObject.Parse(await File.ReadAllTextAsync(metadataFile.FullName));

                //Pick a title from of the possible fields
                var title = metadata["track"]?.Value<string>()
                            ?? metadata["alt_title"]?.Value<string>()
                            ?? metadata["full_title"]?.Value<string>()
                            ?? metadata["title"]?.Value<string>();

                if (title == null)
                    return new FailedDownloadResult(YoutubeDownloadStatus.FailedInvalidDownloadResult);

                //Find a thumbnail
                var thumbnail = (string?)null;
                var thumbnails = (JArray?)metadata["thumbnails"];
                if (thumbnails is {Count: > 0})
                    thumbnail = thumbnails[0]["url"]?.Value<string>();

                //Find artist
                var artist = metadata["artist"]?.Value<string>();

                var durationStr = metadata["duration"]?.Value<string>();
                if (durationStr == null)
                    return new FailedDownloadResult(YoutubeDownloadStatus.FailedInvalidDownloadResult);

                //Find length
                var duration = TimeSpan.FromSeconds(int.Parse(durationStr));

                //Delete temp files
                metadataFile.Delete();

                return new SuccessfulDownloadResult(
                    new YoutubeFile(
                        audioFile,
                        title,
                        url.ToString(),
                        thumbnail,
                        string.IsNullOrWhiteSpace(artist) ? Array.Empty<string>() : new[] {artist!},
                        duration
                    )
                );

            }
            catch (Exception)
            {
                foreach (var item in new DirectoryInfo(_inProgressDownloadFolder).EnumerateFiles($"{tempDownFile}.*"))
                {
                    try
                    {
                        item.Delete();
                    }
                    catch (Exception)
                    {
                        //If we fail to delete the file there's not a lot we can do, just move on
                    }
                }

                throw;
            }
        }

        public async Task<int> PerformMaintenance()
        {
            const string args = "--update";

            //Lock on the mutex to ensure no downloads are in flight
            using (await _mutex.LockAsync())
            {
                return await AsyncProcess.StartProcess(
                    Path.GetFullPath(_youtubeDlBinaryPath),
                    args,
                    Path.GetFullPath(_inProgressDownloadFolder)
                );
            }
        }

        private class FailedDownloadResult
            : IYoutubeDownloadResult
        {
            public FailedDownloadResult(YoutubeDownloadStatus status)
            {
                Status = status;
            }

            public IYoutubeFile? File => null;

            public YoutubeDownloadStatus Status { get; }
        }

        private class SuccessfulDownloadResult
            : IYoutubeDownloadResult
        {
            public SuccessfulDownloadResult(IYoutubeFile file)
            {
                File = file;
            }

            public IYoutubeFile File { get; }

            public YoutubeDownloadStatus Status => YoutubeDownloadStatus.Success;
        }

        private class YoutubeFile
            : IYoutubeFile
        {
            public YoutubeFile(FileInfo file, string title, string url, string? thumbnail, IReadOnlyList<string> artists, TimeSpan duration)
            {
                File = file;
                Title = title;
                Url = url;
                ThumbnailUrl = thumbnail;
                Artists = artists;
                Duration = duration;
            }

            public FileInfo File { get; }

            public string Title { get; }

            public string Url { get; }

            public string? ThumbnailUrl { get; }

            public IReadOnlyList<string> Artists { get; }

            public TimeSpan Duration { get; }

            public void Dispose()
            {
                File.Delete();
            }
        }
    }
}