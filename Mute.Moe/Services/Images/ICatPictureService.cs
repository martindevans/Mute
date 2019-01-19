using System.IO;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Images
{
    public interface ICatPictureService
    {
        Task<Stream> GetCatPictureAsync();
    }
}
