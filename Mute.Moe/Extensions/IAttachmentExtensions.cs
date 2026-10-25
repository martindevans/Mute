using Discord;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using SkiaSharp;

namespace Mute.Moe.Extensions;

/// <summary>
/// Extensions for <see cref="IAttachment"/>
/// </summary>
public static class IAttachmentExtensions
{
    /// <summary>
    /// Try to get a PNG stream from this attachment. Returns null if the attachment is not an image. Transcodes to PNG if it's some other image format.
    /// </summary>
    /// <param name="attachment"></param>
    /// <param name="http"></param>
    /// <returns></returns>
    public static async Task<Stream?> GetPngStream(this IAttachment attachment, HttpClient http)
    {
        // Get the HTTP stream
        var input = await http.GetStreamAsync(attachment.Url);

        // If it's already a PNG, return it
        if (attachment.ContentType.Equals("image/png", StringComparison.OrdinalIgnoreCase))
            return input;

        await using (input)
        {
            // If it's not an image at all, give up
            if (!attachment.ContentType.StartsWith("image/"))
                return null;

            // Decode the image, whatever format it is
            using var bitmap = SKBitmap.Decode(input);
            if (bitmap == null)
                return null;

            // Encode to PNG (give up on failure)
            var output = new MemoryStream();
            if (!bitmap.Encode(output, SKEncodedImageFormat.Png, 100))
            {
                await output.DisposeAsync();
                return null;
            }

            // Return the new PNG stream
            output.Position = 0;
            return output;
        }
    }

    /// <summary>
    /// Get a stream to the contents of this attachment
    /// </summary>
    /// <param name="attachment"></param>
    /// <param name="http"></param>
    /// <returns></returns>
    public static async Task<Stream> GetStream(this IAttachment attachment, HttpClient http)
    {
        return await http.GetStreamAsync(attachment.Url);
    }
}