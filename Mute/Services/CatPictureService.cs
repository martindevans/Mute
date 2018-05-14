using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Mute.Services
{
    public class CatPictureService
    {
        public async Task<Stream> GetCatPictureAsync()
        {
            using (var http = new HttpClient())
            {
                var resp = await http.GetAsync("https://cataas.com/cat");
                return await resp.Content.ReadAsStreamAsync();
            }
        }
    }
}
