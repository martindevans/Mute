using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute;
using Mute.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;

namespace MuteUnitTests
{
    [TestClass]
    public class CatPictureServiceTests
    {
        private static readonly Dictionary<string, string> _responses = new Dictionary<string, string> {
            { "cats", "cats" },
            { "foo", "bar" }
        };

        [TestMethod]
        public async Task Service_Returns_NonZero()
        {
            var httpClient = new MuteTestHttpClient(_responses);
            var cats = new CatPictureService(httpClient, "cats");
            var stream = await cats.GetCatPictureAsync();

            Assert.AreNotEqual(stream.Length, 0);
        }

        [TestMethod]
        public async Task Service_Returns_Zero()
        {
            var httpClient = new MuteTestHttpClient(_responses);
            var cats = new CatPictureService(httpClient, "");
            var stream = await cats.GetCatPictureAsync();

            Assert.AreEqual(stream.Length, 0);
        }
    }

}


