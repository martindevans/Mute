using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute;
using Mute.Services;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MuteUnitTests
{
    [TestClass]
    public class CatPictureServiceTests
    {
        [TestMethod]
        public void Service_Returns_NonZero()
        {
            Stream _stream;
            var t = Task.Run(async() =>
            {
                var _httpClient = new MuteTestHttpClient();
                var _cats = new CatPictureService(_httpClient, "cats");
                _stream = await _cats.GetCatPictureAsync();

                Assert.AreNotEqual(_stream.Length, 0);
            });
            t.Wait();
        }
    }
}
