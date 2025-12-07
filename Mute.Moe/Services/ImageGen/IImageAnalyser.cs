using System.IO;
using System.Threading.Tasks;
using SixLabors.ImageSharp.Formats.Png;

namespace Mute.Moe.Services.ImageGen;

/// <summary>
/// Analyse an image to provide string description of it
/// </summary>
public interface IImageAnalyser
{
    /// <summary>
    /// Get the name of the model being used for analysis
    /// </summary>
    public string ModelName { get; }

    /// <summary>
    /// Indicates if this image analyser is using a local/private model, or a remote service
    /// </summary>
    public bool IsLocal { get; }

    /// <summary>
    /// Given an image, describe it.
    /// </summary>
    /// <param name="image"></param>
    /// <returns></returns>
    public Task<ImageAnalysisResult?> GetImageDescription(Stream image);

    /// <summary>
    /// Given an image, describe it
    /// </summary>
    /// <param name="image"></param>
    /// <returns></returns>
    public async Task<ImageAnalysisResult?> GetImageDescription(SixLabors.ImageSharp.Image image)
    {
        var mem = new MemoryStream();
        await image.SaveAsync(mem, new PngEncoder());
        mem.Position = 0;

        return await GetImageDescription(mem);
    }
}

/// <summary>
/// Result from image analyser
/// </summary>
/// <param name="Title"></param>
/// <param name="Description"></param>
public record ImageAnalysisResult(string? Title, string Description);