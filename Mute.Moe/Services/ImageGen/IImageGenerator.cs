using System.IO;
using System.Threading.Tasks;
using Image = SixLabors.ImageSharp.Image;

namespace Mute.Moe.Services.ImageGen;

public interface IImageGenerator
{
    Task<IReadOnlyCollection<Image>> Text2Image(int? seed, string positive, string negative, Func<ProgressReport, Task>? progress = null, int batch = 1);

    Task<IReadOnlyCollection<Image>> Image2Image(int? seed, Image image, string positive, string negative, Func<ProgressReport, Task>? progress = null, int batch = 1);

    public record struct ProgressReport(float Progress, MemoryStream? Intermediate);
}