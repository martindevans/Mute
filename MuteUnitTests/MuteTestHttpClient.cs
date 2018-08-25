using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Mute
{
    class MuteTestHttpClient
        : IHttpClient
    {
        private readonly Dictionary<string, string> _responses = new Dictionary<string, string>();
        private static string _defaultResponse;

        public MuteTestHttpClient(Dictionary<string, string> responses, string defaultResponse = "")
        {
            _responses = responses;
            _defaultResponse = defaultResponse;            
        }

        public Task<HttpResponseMessage> GetAsync(string uri)
        {
            HttpResponseMessage GenerateResponse()
            {
                if (!_responses.TryGetValue(uri, out var response))
                    response = _defaultResponse;
                return new HttpResponseMessage { Content = new StringContent(response) };
            }
            return Task.Run((Func<HttpResponseMessage>)GenerateResponse);
        }
    }
}