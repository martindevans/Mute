using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Services;
using Mute.Tests.Mocks;

namespace Mute.Tests
{
    [TestClass]
    public class DogPictureServiceTests
    {
        private static readonly Dictionary<string, HttpResponseMessage> Responses = new Dictionary<string, HttpResponseMessage> {
            { "https://images.dog.ceo/breeds/elkhound-norwegian/n02091467_4951.jpg", S("a picture of a default dog") },
            { "test_dog_url", S("{ \"status\":\"ok\", \"message\":\"test_dog_response\" }") },
            { "broken_dog_url", S("") },
            { "test_dog_response", S("a picture of a dog") }
        };

        private static HttpResponseMessage S(string str)
        {
            return new HttpResponseMessage { Content = new StringContent(str) };
        }

        [TestMethod]
        public async Task Service_Returns_Dog()
        {
            var httpClient = new MockHttpClient(Responses);
            var dogs = new DogPictureService(httpClient, "test_dog_url");
            var stream = await dogs.GetDogPictureAsync();
            var response = new StreamReader(stream).ReadToEnd();

            Assert.AreEqual(await Responses["test_dog_response"].Content.ReadAsStringAsync(), response);
        }

        [TestMethod]
        public async Task Service_Returns_DefaultValue()
        {
            var httpClient = new MockHttpClient(Responses);
            var dogs = new DogPictureService(httpClient, "broken_dog_url");
            var stream = await dogs.GetDogPictureAsync();
            var response = new StreamReader(stream).ReadToEnd();

            Assert.AreEqual("a picture of a default dog", response);
        }
    }
}

