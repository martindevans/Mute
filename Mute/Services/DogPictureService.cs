using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Mute.Services
{
    public class DogPictureService
    {
        public async Task<Stream> GetDogPictureAsync()
        {
            using (var http = new HttpClient())
            {
                //Ask API for a dog image
                var httpResp = await http.GetAsync("https://dog.ceo/api/breeds/image/random");
                var jsonResp = JsonConvert.DeserializeObject<Response>(await httpResp.Content.ReadAsStringAsync());

                // Fetch dog image
                var imgHttpResp = await http.GetAsync(jsonResp.message);
                return await imgHttpResp.Content.ReadAsStreamAsync();
            }
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class Response
        {
            // ReSharper disable once InconsistentNaming
            public string status;

            // ReSharper disable once InconsistentNaming
            public string message;
        }
    }
}
