using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Mute.Moe.Utilities;

namespace Mute.Moe.Services.Images.Cats
{
    public class ThisCatDoesNotExist
        : IArtificialCatPictureProvider
    {
        private const string URL = "https://thiscatdoesnotexist.com/";
        private readonly IHttpClient _client;

        public ThisCatDoesNotExist(IHttpClient client)
        {
            _client = client;
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
