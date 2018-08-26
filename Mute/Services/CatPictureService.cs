using System.IO;
using System.Threading.Tasks;

namespace Mute.Services
{
    public class CatPictureService
    {
        private readonly string _url;
        private readonly IHttpClient _client;

        public CatPictureService(IHttpClient client, string url = "https://cataas.com/cat")
        {
            _url = url;
            _client = client;
        }

        public async Task<Stream> GetCatPictureAsync()
        {
            var resp = await _client.GetAsync(_url);
            return await resp.Content.ReadAsStreamAsync();
        }
    }
}
