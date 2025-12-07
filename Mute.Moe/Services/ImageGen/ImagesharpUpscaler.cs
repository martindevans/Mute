using System.Threading.Tasks;
using Autofocus.Outpaint;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;

namespace Mute.Moe.Services.ImageGen;

/// <summary>
/// Upscale images using ImageSharp
/// </summary>
public class ImagesharpUpscaler
    : IImageUpscaler
{
    /// <inheritdoc />
    public async Task<Image> UpscaleImage(Image image, uint width, uint height, Func<ProgressReport, Task>? progressReporter = null)
    {
        return image.Clone(ctx => ctx.Resize(new ResizeOptions()
        {
            Size = new Size((int)width, (int)height),
            Sampler = CubicResampler.MitchellNetravali
        }));
    }
}