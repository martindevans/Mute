using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Metadata;

namespace Mute.Moe.Extensions;

/// <summary>
/// Extensions related to images
/// </summary>
public static class ImageExtensions
{
    /// <summary>
    /// Get Automatic1111 image generation metadata embedded in this image
    /// </summary>
    /// <param name="meta"></param>
    /// <returns></returns>
    public static string? GetGenerationMetadata(this PngMetadata meta)
    {
        var parameters = meta.TextData.FirstOrDefault(a => a.Keyword == "parameters");

        if (string.IsNullOrEmpty(parameters.Value))
            return null;

        return parameters.Value;
    }

    /// <summary>
    /// Get Automatic1111 image generation prompt embedded in this image
    /// </summary>
    /// <param name="image"></param>
    /// <returns></returns>
    public static (string, string)? GetGenerationPrompt(this Image image)
    {
        return image.Metadata.GetGenerationPrompt();
    }

    /// <summary>
    /// Get Automatic1111 image generation prompt embedded in this image
    /// </summary>
    /// <param name="meta"></param>
    /// <returns></returns>
    public static (string, string)? GetGenerationPrompt(this ImageMetadata meta)
    {
        var pngMeta = meta.GetPngMetadata();
        return pngMeta.GetGenerationPrompt();
    }

    /// <summary>
    /// Get Automatic1111 image generation prompt embedded in this image
    /// </summary>
    /// <param name="meta"></param>
    /// <returns></returns>
    public static (string, string)? GetGenerationPrompt(this PngMetadata meta)
    {
        var parameters = GetGenerationMetadata(meta);
        if (string.IsNullOrEmpty(parameters))
            return null;

        var lines = parameters.Split('\n');

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
}