using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Mute;

namespace Mute
{
    class MuteTestHttpClient : IHttpClient
    {
        string _uri;
        // Am I living in the fucking twilight zone?
        private HttpResponseMessage _generateResponse()
        {
            var responses = new HttpResponseMessage();
            responses.Content = new StringContent(_uri);

            return responses;
        }

        public Task<HttpResponseMessage> GetAsync(string uri)
        {
            _uri = "";
            Func<HttpResponseMessage> generateResponse = _generateResponse;
            return new Task<HttpResponseMessage>(generateResponse);
        }
    }
}
