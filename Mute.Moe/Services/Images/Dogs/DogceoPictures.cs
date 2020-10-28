using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Mute.Moe.Services.Images.Dogs
{
    public class DogceoPictures
        : IDogPictureService
    {
        private readonly string _url;
        private readonly HttpClient _client;

        public DogceoPictures(IHttpClientFactory client, string url = "https://dog.ceo/api/breeds/image/random")
        {
            _url = url;
            _client = client.CreateClient();
        }

        public async Task<Stream> GetDogPictureAsync()
        {
            //Ask API for a dog image
            var httpResp = await _client.GetAsync(_url);
            var jsonResp = JsonConvert.DeserializeObject<Response?>(await httpResp.Content.ReadAsStringAsync());

            // Fetch dog image, If there is no message, 
            // return a default image. (From their api)
            using var imgHttpResp = await _client.GetAsync(jsonResp?.message ?? "https://images.dog.ceo/breeds/elkhound-norwegian/n02091467_4951.jpg");
            var m = new MemoryStream();
            await imgHttpResp.Content.CopyToAsync(m);
            m.Position = 0;
            return m;
        }

        #pragma warning disable CS0649
        // ReSharper disable once ClassNeverInstantiated.Local
        private class Response
        {
            // ReSharper disable once InconsistentNaming
            [UsedImplicitly] public string? status;

            // ReSharper disable once InconsistentNaming
            [UsedImplicitly] public string? message;
        }
        #pragma warning restore CS0649
    }
}
