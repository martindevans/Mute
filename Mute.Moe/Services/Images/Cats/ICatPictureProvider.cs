using System.IO;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Images.Cats
{
    public interface ICatPictureProvider
    {
        Task<Stream> GetCatPictureAsync();
    }
}
