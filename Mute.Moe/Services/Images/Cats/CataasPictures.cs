using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Mute.Moe.Utilities;

namespace Mute.Moe.Services.Images.Cats
{
    public class CataasPictures
        : ICatPictureProvider
    {
        private const string URL = "https://cataas.com/cat";
        private readonly HttpClient _client;

        public CataasPictures(IHttpClientFactory client)
        {
            _client = client.CreateClient();
        }

        [ItemNotNull]
        public async Task<Stream> GetCatPictureAsync()
        {
            using (var resp = await _client.GetAsync(URL))
            {
                var m = new MemoryStream();
                await resp.Content.CopyToAsync(m);
                m.Position = 0;
                return m;
            }
        }
    }
}
