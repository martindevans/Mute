using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Services;
using Mute.Tests.Mocks;

namespace Mute.Tests.Services
{
    [TestClass]
    public class CatPictureServiceTests
    {
        private static readonly Dictionary<string, HttpResponseMessage> _responses = new Dictionary<string, HttpResponseMessage> {
            { "cats", S("cats") }
        };

        private static HttpResponseMessage S(string str)
        {
            return new HttpResponseMessage { Content = new StringContent(str) };
        }

        [TestMethod]
        public async Task Service_Returns_NonZero()
        {
            var httpClient = new MockHttpClient(_responses);
            var cats = new CatPictureService(httpClient, "cats");
            var stream = await cats.GetCatPictureAsync();

            Assert.AreEqual(4, stream.Length);
        }
    }

}


