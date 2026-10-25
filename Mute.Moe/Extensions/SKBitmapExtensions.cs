using SkiaSharp;

namespace Mute.Moe.Extensions;

/// <summary>
/// Extensions to <see cref="SKBitmap"/>
/// </summary>
public static class SKBitmapExtensions
{
    /// <summary>
    /// Resize an image to fit into the given max size (scaling up or down). Preserving aspect ratio.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="maxWidth"></param>
    /// <param name="maxHeight"></param>
    /// <returns></returns>
    public static SKBitmap ResizeFitWithin(this SKBitmap input, int maxWidth, int maxHeight)
    {
        var w = input.Width;
        var h = input.Height;

        var scale = Math.Min((float)maxWidth / w, (float)maxHeight / h);

        var w2 = Math.Min((int)Math.Round(w * scale), maxWidth);
        var h2 = Math.Min((int)Math.Round(h * scale), maxHeight);

        return input.Resize(
            new SKImageInfo(w2, h2, input.ColorType, input.AlphaType),
            new SKSamplingOptions(SKCubicResampler.Mitchell)
        );
    }
}