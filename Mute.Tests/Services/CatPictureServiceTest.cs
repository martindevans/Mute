using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Moe.Discord.Services;
using Mute.Moe.Services.Images;
using Mute.Tests.Mocks;

namespace Mute.Tests.Services
{
    [TestClass]
    public class CatPictureServiceTests
    {
        private static readonly Dictionary<string, HttpResponseMessage> Responses = new Dictionary<string, HttpResponseMessage> {
            { "cats", S("cats") }
        };

        [NotNull]
        private static HttpResponseMessage S(string str)
        {
            return new HttpResponseMessage { Content = new StringContent(str) };
        }

        [TestMethod]
        public async Task Service_Returns_NonZero()
        {
            var httpClient = new MockHttpClient(Responses);
            var cats = new CataasPictures(httpClient, "cats");
            var stream = await cats.GetCatPictureAsync();

            Assert.AreEqual(4, stream.Length);
        }
    }

}


