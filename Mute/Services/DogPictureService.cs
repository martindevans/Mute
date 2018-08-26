using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Mute.Services
{
    public class DogPictureService
    {
        private readonly string _url;
        private readonly IHttpClient _client;

        public DogPictureService(IHttpClient client, string url = "https://dog.ceo/api/breeds/image/random")
        {
            _url = url;
            _client = client;
        }

        public async Task<Stream> GetDogPictureAsync()
        {
                //Ask API for a dog image
                var httpResp = await _client.GetAsync(_url);
                var jsonResp = JsonConvert.DeserializeObject<Response>(await httpResp.Content.ReadAsStringAsync());

                // Fetch dog image, If there is no message, 
                // return a default image. (From their api)
                var imgHttpResp = await _client.GetAsync(jsonResp?.message ?? "https://images.dog.ceo/breeds/elkhound-norwegian/n02091467_4951.jpg");
                return await imgHttpResp.Content.ReadAsStreamAsync();
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class Response
        {
            // ReSharper disable once InconsistentNaming
            public string status;

            public string message;
            // ReSharper disable once InconsistentNaming
        }
    }
}
