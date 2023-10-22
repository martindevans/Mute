using System.Threading.Tasks;
using Autofocus.Outpaint;
using Image = SixLabors.ImageSharp.Image;

namespace Mute.Moe.Services.ImageGen;

public interface IImageGenerator
{
    Task<IReadOnlyCollection<Image>> Text2Image(int? seed, Prompt prompt, Func<ProgressReport, Task>? progress = null, int batch = 1);

    Task<IReadOnlyCollection<Image>> Image2Image(int? seed, Image image, Prompt prompt, Func<ProgressReport, Task>? progress = null, int batch = 1);

    //public record struct ProgressReport(float Progress, MemoryStream? Intermediate);
}

public record Prompt
{
    public required string Positive { get; set; }
    public required string Negative { get; set; }

    public string? FaceEnhancementPositive { get; set; }
    public string? FaceEnhancementNegative { get; set; }

    public string? EyeEnhancementPositive { get; set; }
    public string? EyeEnhancementNegative { get; set; }

    public string? HandEnhancementPositive { get; set; }
    public string? HandEnhancementNegative { get; set; }
}