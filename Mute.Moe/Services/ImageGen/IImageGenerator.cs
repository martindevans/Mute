using System;
using System.IO;
using System.Threading.Tasks;

namespace Mute.Moe.Services.ImageGen
{
    public interface IImageGenerator
    {
        Task<Stream> GenerateImage(int seed, string positive, string negative, Func<ProgressReport, Task>? progress = null);

        public record struct ProgressReport(float Progress, MemoryStream? Intermediate);
    }
}
