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

        public Task<HttpResponseMessage> GetAsync(string uri)
        {
            return _client.GetAsync(uri);
        }
    }
}
