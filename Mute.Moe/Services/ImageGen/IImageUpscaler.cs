using System.Threading.Tasks;
using Autofocus.Outpaint;
using SixLabors.ImageSharp;

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
    public Task<Image> UpscaleImage(Image image, uint width, uint height, Func<ProgressReport, Task>? progressReporter = null);
}