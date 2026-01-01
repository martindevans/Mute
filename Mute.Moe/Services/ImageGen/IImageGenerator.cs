using System.Threading.Tasks;
using Autofocus.Outpaint;
using Image = SixLabors.ImageSharp.Image;

namespace Mute.Moe.Services.ImageGen;

/// <summary>
/// Service to generate images from a prompt
/// </summary>
public interface IImageGenerator
{
    /// <summary>
    /// Generate an image from a text prompt
    /// </summary>
    /// <param name="seed"></param>
    /// <param name="prompt"></param>
    /// <param name="progress"></param>
    /// <param name="batch"></param>
    /// <returns></returns>
    Task<IReadOnlyCollection<Image>> Text2Image(int? seed, Prompt prompt, Func<ProgressReport, Task>? progress = null, int batch = 1);

    /// <summary>
    /// Generate an image from an image prompt and a text prompt
    /// </summary>
    /// <param name="seed"></param>
    /// <param name="image"></param>
    /// <param name="prompt"></param>
    /// <param name="progress"></param>
    /// <param name="batch"></param>
    /// <returns></returns>
    Task<IReadOnlyCollection<Image>> Image2Image(int? seed, Image image, Prompt prompt, Func<ProgressReport, Task>? progress = null, int batch = 1);
}

/// <summary>
/// Prompt for image generation
/// </summary>
public record Prompt
{
    /// <summary>
    /// Positive prompt
    /// </summary>
    public required string Positive { get; set; }

    /// <summary>
    /// Negative prompt
    /// </summary>
    public required string Negative { get; set; }

    /// <summary>
    /// Positive prompt for face
    /// </summary>
    public string? FaceEnhancementPositive { get; set; }

    /// <summary>
    /// Negative prompt for face
    /// </summary>
    public string? FaceEnhancementNegative { get; set; }

    /// <summary>
    /// Positive prompt for eyes
    /// </summary>
    public string? EyeEnhancementPositive { get; set; }

    /// <summary>
    /// Negative prompt for eyes
    /// </summary>
    public string? EyeEnhancementNegative { get; set; }

    /// <summary>
    /// Positive prompt for hands
    /// </summary>
    public string? HandEnhancementPositive { get; set; }

    /// <summary>
    /// Negative prompt for hands
    /// </summary>
    public string? HandEnhancementNegative { get; set; }
}