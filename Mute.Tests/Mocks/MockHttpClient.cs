using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Mute.Tests.Mocks
{
    internal class MockHttpClient
        : IHttpClient
    {
        private readonly Dictionary<string, HttpResponseMessage> _responses;
        private static HttpResponseMessage _defaultResponse;

        public MockHttpClient(Dictionary<string, HttpResponseMessage> responses, [CanBeNull] HttpResponseMessage defaultResponse = null)
        {
            _responses = responses;
            _defaultResponse = defaultResponse ?? new HttpResponseMessage(HttpStatusCode.NotFound);            
        }

        public Task<HttpResponseMessage> GetAsync(string uri)
        {
            HttpResponseMessage GenerateResponse()
            {
                if (!_responses.TryGetValue(uri, out var response))
                    response = _defaultResponse;
                return response;
            }

            return Task.Run((Func<HttpResponseMessage>)GenerateResponse);
        }
    }
}