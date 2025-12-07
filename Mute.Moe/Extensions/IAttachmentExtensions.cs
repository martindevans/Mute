using Discord;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using SixLabors.ImageSharp;

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
        var input = await http.GetStreamAsync(attachment.Url);
        if (attachment.ContentType == "image/png")
            return input;

        if (!attachment.ContentType.StartsWith("image/"))
            return null;

        var image = await SixLabors.ImageSharp.Image.LoadAsync(input);

        var output = new MemoryStream();
        await image.SaveAsPngAsync(output);

        output.Position = 0;
        return output;
    }
}