using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Images.Cats
{
    public class ThisCatDoesNotExist
        : IArtificialCatPictureProvider
    {
        private const string URL = "https://thiscatdoesnotexist.com/";
        private readonly HttpClient _client;

        public ThisCatDoesNotExist(IHttpClientFactory client)
        {
            _client = client.CreateClient();
        }

        public async Task<Stream> GetCatPictureAsync()
        {
            using var resp = await _client.GetAsync(URL);
            var m = new MemoryStream();
            await resp.Content.CopyToAsync(m);
            m.Position = 0;
            return m;
        }
    }
}
