using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;

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

        [ItemNotNull]
        public async Task<Stream> GetCatPictureAsync()
        {
            using (var resp = await _client.GetAsync(_url))
            {
                var m = new MemoryStream();
                await resp.Content.CopyToAsync(m);
                m.Position = 0;
                return m;
            }
        }
    }
}
