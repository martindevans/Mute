using Autofocus.Utilities.Progress;
using SkiaSharp;
using System.Threading.Tasks;

namespace Mute.Moe.Services.ImageGen;

/// <summary>
/// Upscale image to a given size
/// </summary>
public interface IImageUpscaler
{
    /// <summary>
    /// Resize an image
    /// </summary>
    /// <param name="image"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="progressReporter"></param>
    /// <returns></returns>
    public Task<SKBitmap> UpscaleImage(SKBitmap image, uint width, uint height, Func<ProgressReport, Task>? progressReporter = null);
}