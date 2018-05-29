using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Mute.Services
{
    public class YoutubeService
    {
        [NotNull] private readonly YoutubeDlConfig _config;

        public YoutubeService([NotNull] Configuration config, [NotNull] DatabaseService database)
        {
            _config = config.YoutubeDl;
        }

        [NotNull] public Uri CheckUrl([NotNull] string urlString)
        {
            //Sanity check that it's a well formed URL
            if (!Uri.TryCreate(urlString, UriKind.Absolute, out var url))
                throw new InvalidOperationException("Cannot download - not a valid URL");

            //Sanity check that the scheme is correct
            if (url.Scheme != "http" && url.Scheme != "https")
                throw new ArgumentException("URL scheme must be http(s)", nameof(urlString));

            //Extra check for escape characters in the URL
            if (urlString.Contains("\"") || urlString.Contains("\'"))
                throw new InvalidOperationException("Cannot download URL - it contains invalid characters");

            return url;
        }

        [ItemCanBeNull] public async Task<FileInfo> GetYoutubeAudio([NotNull] string youtubeUrl)
        {
            return await DownloadYoutube(CheckUrl(youtubeUrl), true);
        }

        [ItemCanBeNull] public async Task<FileInfo> GetYoutubeVideo([NotNull] string youtubeUrl)
        {
            return await DownloadYoutube(CheckUrl(youtubeUrl), false);
        }

        [ItemCanBeNull] private async Task<FileInfo> DownloadYoutube([NotNull] Uri youtubeUrl, bool extractAudio)
        {
            //Build args
            var fileName = Guid.NewGuid().ToString();
            var downloadingLocation = Path.GetFullPath(Path.Combine(_config.InProgressDownloadFolder, fileName));
            var args = $"\"{youtubeUrl}\" --no-check-certificate --output \"{downloadingLocation}.`%(ext)s\" --quiet --no-warnings --limit-rate {_config.RateLimit ?? "2.5M"}";
            if (extractAudio)
                args += " --extract-audio --audio-format wav";

            Console.WriteLine(args);

            //Download to the in progress download folder
            try
            {
                await AsyncProcess.StartProcess(
                    Path.GetFullPath(_config.YoutubeDlBinaryPath), 
                    args,
                    Path.GetFullPath(_config.InProgressDownloadFolder)
                );
            }
            catch (TaskCanceledException)
            {
                return null;
            }

            //Find the completed download (we don't know the extension, so search for it by name)
            var maybeCompleteFile = Directory.GetFiles(Path.GetFullPath(_config.InProgressDownloadFolder), fileName + ".*").SingleOrDefault();

            //Early exit if it doesn't exist (download failed)
            if (maybeCompleteFile == null)
                return null;

            //Move to completed folder
            var completedDownload = new FileInfo(maybeCompleteFile);
            var finalLocation = new FileInfo(Path.Combine(_config.CompleteDownloadFolder, completedDownload.Name));
            completedDownload.MoveTo(finalLocation.FullName);

            return finalLocation;
        }
    }
}
