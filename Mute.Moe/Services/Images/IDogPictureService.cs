using System.IO;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Images
{
    public interface IDogPictureService
    {
        Task<Stream> GetDogPictureAsync();
    }
}