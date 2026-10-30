using Autofocus.Utilities.Progress;
using SkiaSharp;
using System.Threading.Tasks;

namespace Mute.Moe.Services.ImageGen;

/// <summary>
/// Upscale images using SkiaSharp
/// </summary>
public class SkiaSharpUpscaler
    : IImageUpscaler
{
    /// <inheritdoc />
    public async Task<SKBitmap> UpscaleImage(SKBitmap image, uint width, uint height, Func<ProgressReport, Task>? progressReporter = null)
    {
        // Preserve the source's color type and alpha type
        var info = new SKImageInfo(
            checked((int)width),
            checked((int)height),
            image.ColorType,
            image.AlphaType
        );

        var sampling = new SKSamplingOptions(SKCubicResampler.Mitchell);

        return image.Resize(info, sampling)
            ?? throw new InvalidOperationException("Resize failed.");
    }
}