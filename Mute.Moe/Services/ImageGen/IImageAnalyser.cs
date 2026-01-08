using SixLabors.ImageSharp.Formats.Png;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
    /// Given an image, describe it.
    /// </summary>
    /// <param name="image"></param>
    /// <param name="cancellation"></param>
    /// <returns></returns>
    public Task<ImageAnalysisResult?> GetImageDescription(Stream image, CancellationToken cancellation = default);

    /// <summary>
    /// Given an image, describe it
    /// </summary>
    /// <param name="image"></param>
    /// <param name="cancellation"></param>
    /// <returns></returns>
    public async Task<ImageAnalysisResult?> GetImageDescription(SixLabors.ImageSharp.Image image, CancellationToken cancellation = default)
    {
        var mem = new MemoryStream();
        await image.SaveAsync(mem, new PngEncoder(), cancellation);
        mem.Position = 0;

        return await GetImageDescription(mem, cancellation);
    }
}

/// <summary>
/// Result from image analyser
/// </summary>
/// <param name="Title"></param>
/// <param name="Description"></param>
public record ImageAnalysisResult(string? Title, string Description);