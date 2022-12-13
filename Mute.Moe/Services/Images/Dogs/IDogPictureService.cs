using System.IO;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Images.Dogs;

public interface IDogPictureService
{
    Task<Stream> GetDogPictureAsync();
}