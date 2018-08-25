using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute;
using Mute.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MuteUnitTests
{
    [TestClass]
    public class DogPictureServiceTests
    {
        private static readonly Dictionary<string, string> _responses = new Dictionary<string, string> {
            { "https://images.dog.ceo/breeds/elkhound-norwegian/n02091467_4951.jpg", "a picture of a default dog" },
            { "test_dog_url", "{ \"status\":\"ok\", \"message\":\"test_dog_response\" }" },
            { "broken_dog_url", "" },
            { "test_dog_response", "a picture of a dog" }
        };


        [TestMethod]
        public async Task Service_Returns_Dog()
        {
            var httpClient = new MuteTestHttpClient(_responses);
            var dogs = new DogPictureService(httpClient, "test_dog_url");
            var stream = await dogs.GetDogPictureAsync();
            var response = new StreamReader(stream).ReadToEnd();

            Assert.AreEqual(_responses["test_dog_response"], response);
        }

        [TestMethod]
        public async Task Service_Returns_DefaultValue()
        {
            var httpClient = new MuteTestHttpClient(_responses);
            var dogs = new DogPictureService(httpClient, "broken_dog_url");
            var stream = await dogs.GetDogPictureAsync();
            var response = new StreamReader(stream).ReadToEnd();

            Assert.AreEqual("a picture of a default dog", response);
        }
    }
}

