using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Mute
{
    class MuteHttpClient : IHttpClient
    {
        HttpClient _client;

        public async Task<HttpResponseMessage> GetAsync(string uri)
        {
            return await _client.GetAsync(uri);
        }
    }
}
