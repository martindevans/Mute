using MetadataExtractor;
using MetadataExtractor.Formats.Png;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SkiaSharp;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Mute.Moe.Extensions;

/// <summary>
/// Extensions related to images
/// </summary>
public static class ImageExtensions
{
    /// <summary>
    /// Returns the value of a PNG text section (tEXt/zTXt/iTXt) with the given keyword,
    /// or null if no matching section exists.
    /// </summary>
    public static string? GetPngTextSection(this Stream stream, string keyword)
    {
        if (stream.Position != 0)
            stream.Seek(0, SeekOrigin.Begin);
        
        var metadata = ImageMetadataReader.ReadMetadata(stream);
        return metadata
              .OfType<PngDirectory>()
              .Select(a => a.GetObject(PngDirectory.TagTextualData))
              .Where(a => a != null)
              .OfType<MetadataExtractor.KeyValuePair[]>()
              .SelectMany(a => a)
              .Where(a => a.Key.Equals(keyword, StringComparison.OrdinalIgnoreCase))
              .Select(a => a.Value.ToString())
              .FirstOrDefault();
    }

    /// <summary>
    /// Assuming this stream is a PNG, retrieve AUTOMATIC1111 image generation metadata
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public static string? GetImageGenerationMetadata(this Stream stream)
    {
        return GetPngTextSection(stream, "parameters");
    }

    /// <summary>
    /// Get Automatic1111 image generation prompt embedded in this image
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public static (string, string)? GetImageGenerationPrompt(this Stream stream)
    {
        return ParseGenerationMetadata(stream.GetImageGenerationMetadata());
    }

    /// <summary>
    /// Get Automatic1111 image generation prompt embedded in this image
    /// </summary>
    /// <param name="image"></param>
    /// <returns></returns>
    public static (string, string)? GetGenerationPrompt(this Image image)
    {
        var pngMeta = image.Metadata.GetPngMetadata();
        string? ret;
        var parameters = pngMeta.TextData.FirstOrDefault(a => a.Keyword == "parameters");

        if (string.IsNullOrEmpty(parameters.Value))
            ret = null;
        else
        {
            ret = parameters.Value;
        }

        return ParseGenerationMetadata(ret);
    }

    private static (string, string)? ParseGenerationMetadata(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return null;

        var lines = input.Split('\n');

        const string npHeader = "Negative prompt: ";
        if (lines[0].StartsWith(npHeader))
            return ("", lines[0]);

        var positive = lines[0];
        var negative = lines[1];

        if (!negative.StartsWith(npHeader))
            return null;
        negative = negative.Remove(0, npHeader.Length);

        return (positive, negative);
    }

    /// <summary>
    /// Convert to an SK Bitmap
    /// </summary>
    /// <param name="image"></param>
    /// <param name="cancellation"></param>
    /// <returns></returns>
    public static async Task<SKBitmap> ToSkBitmap(this Image image, CancellationToken cancellation = default)
    {
        using var mem = new MemoryStream();
        await image.SaveAsPngAsync(mem, cancellation);
        mem.Position = 0;

        return SKBitmap.Decode(mem)
            ?? throw new InvalidOperationException("SkiaSharp failed to decode the image.");
    }
}