using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Metadata;

namespace Mute.Moe.Extensions;

/// <summary>
/// Extensions related to images
/// </summary>
public static class ImageExtensions
{
    public static string? GetGenerationMetadata(this PngMetadata pngMeta)
    {
        var parameters = pngMeta.TextData.FirstOrDefault(a => a.Keyword == "parameters");

        if (string.IsNullOrEmpty(parameters.Value))
            return null;

        return parameters.Value;
    }


    public static (string, string)? GetGenerationPrompt(this Image image)
    {
        return image.Metadata.GetGenerationPrompt();
    }

    public static (string, string)? GetGenerationPrompt(this ImageMetadata meta)
    {
        var pngMeta = meta.GetPngMetadata();
        return pngMeta.GetGenerationPrompt();
    }

    public static (string, string)? GetGenerationPrompt(this PngMetadata pngMeta)
    {
        var parameters = GetGenerationMetadata(pngMeta);
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