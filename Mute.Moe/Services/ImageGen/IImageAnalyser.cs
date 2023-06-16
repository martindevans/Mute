using System.IO;
using System.Threading.Tasks;

namespace Mute.Moe.Services.ImageGen
{
    public interface IImageAnalyser
    {
        public Task<string> GetImageDescription(Stream image);
    }
}
