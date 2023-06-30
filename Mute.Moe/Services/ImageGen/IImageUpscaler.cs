using System.Threading.Tasks;
using SixLabors.ImageSharp;

namespace Mute.Moe.Services.ImageGen
{
    public interface IImageUpscaler
    {
        public Task<Image?> UpscaleImage(Image image, uint width, uint height, Func<IImageGenerator.ProgressReport, Task>? progressReporter = null);
    }
}
