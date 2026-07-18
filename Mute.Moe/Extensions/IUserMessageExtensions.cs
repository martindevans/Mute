using System.IO;
using Discord;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Mute.Moe.Extensions;

/// <summary>
/// Extensions for <see cref="IUserMessage"/>
/// </summary>
public static class IUserMessageExtensions
{
    /// <summary>
    /// Get all image attachments from this message or the message it references
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public static IReadOnlyList<IAttachment> GetMessageImageAttachments(this IMessage message)
    {
        // Get all attachments for message
        var attachments = message.Attachments.ToList<IAttachment>();

        if (message is IUserMessage um)
        {
            // Get all attachments from mentioned message (if any)
            attachments.AddRange(um.ReferencedMessage?.Attachments ?? []);
        }

        // Remove all non image attachments
        attachments.RemoveAll(a => !a.ContentType.StartsWith("image/"));

        return attachments;
    }

    /// <summary>
    /// Get all image attachments from this message or the message it references
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public static IReadOnlyList<IAttachment> GetMessageImageAttachments(this IUserMessage message)
    {
        return ((IMessage)message).GetMessageImageAttachments();
    }

    /// <summary>
    /// Get the URLs of all images in this message. Attachments, embeds and referenced messages
    /// </summary>
    /// <param name="message"></param>
    /// <param name="followReference"></param>
    /// <returns></returns>
    public static List<string> GetMessageImageUrls(this IUserMessage message, bool followReference = true)
    {
        // Get all image attachments
        var urls = message.Attachments.Where(a => a.ContentType.StartsWith("image/")).Select(a => a.Url).ToList();

        // Add Image embeds
        var embeds = message.Embeds
                            .Where(a => a.Type == EmbedType.Image)
                            .Select(a => a.Image?.ProxyUrl ?? a.Image?.Url ?? a.Url)
                            .Where(a => a != null)
                            .Select(a => a!);
        urls.AddRange(embeds);

        // Load from referenced message
        if (followReference && message.ReferencedMessage != null)
            urls.AddRange(message.ReferencedMessage.GetMessageImageUrls(false));

        return urls;
    }

    /// <summary>
    /// Get all images from this message or the message it references
    /// </summary>
    /// <param name="message"></param>
    /// <param name="http"></param>
    /// <param name="cancellation"></param>
    /// <returns></returns>
    public static async Task<IReadOnlyList<MemoryStream>> GetMessageImageStreams(this IUserMessage message, HttpClient http, CancellationToken cancellation = default)
    {
        // Find the image URLs
        var images = message.GetMessageImageUrls();

        // Try to fetch them all
        var streams = (await Task.WhenAll(images.Select(url => FetchOneImage(url, cancellation))))
            .Where(a => a != null)
            .Select(a => a!)
            .ToList();

        return streams;

        async Task<MemoryStream?> FetchOneImage(string url, CancellationToken ct)
        {
            try
            {
                using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
                if (!response.IsSuccessStatusCode)
                    return null;

                var array = await response.Content.ReadAsByteArrayAsync(ct);
                return new MemoryStream(array, writable: false);
            }
            catch (IOException)
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Get all images from this message or the message it references
    /// </summary>
    /// <param name="message"></param>
    /// <param name="http"></param>
    /// <returns></returns>
    public static async Task<IReadOnlyList<SixLabors.ImageSharp.Image>> GetMessageImages(this IUserMessage message, HttpClient http)
    {
        var images = message.GetMessageImageUrls();

        var result = await images
                          .ToAsyncEnumerable()
                          .Select(async (url, ct) => await http.GetAsync(url, ct))
                          .Where(IsSuccess)
                          .Select(async (resp, ct) => await SixLabors.ImageSharp.Image.LoadAsync(await resp.Content.ReadAsStreamAsync(ct), ct))
                          .ToListAsync();

        return result;

        static bool IsSuccess(HttpResponseMessage message)
        {
            if (!message.IsSuccessStatusCode)
                message.Dispose();
            return message.IsSuccessStatusCode;
        }
    }
}