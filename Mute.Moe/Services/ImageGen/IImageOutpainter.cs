using System.Threading.Tasks;
using Autofocus.Outpaint;
using SixLabors.ImageSharp;

namespace Mute.Moe.Services.ImageGen;

/// <summary>
/// Outpaint an image
/// </summary>
public interface IImageOutpainter
{
    /// <summary>
    /// Do outpainting on the given image
    /// </summary>
    /// <param name="image"></param>
    /// <param name="positive"></param>
    /// <param name="negative"></param>
    /// <param name="progressReporter"></param>
    /// <returns></returns>
    public Task<IReadOnlyCollection<Image>> Outpaint(Image image, string positive, string negative, Func<ProgressReport, Task>? progressReporter = null);
}