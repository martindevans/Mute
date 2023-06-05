using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Mute.Moe.Services.ImageGen
{
    public interface IImageGenerator
    {
        Task<Stream> GenerateImage(int seed, string positive, string negative);
    }
}
