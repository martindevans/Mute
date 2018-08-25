using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Mute.Services;
using System.Threading;
using System.Text;
using System.Net;

namespace MuteTests
{
    

    [TestClass]
    public class ServiceTests
    {
        HttpResponseMessage foo;

        // Check that the Cat Picture service still returns something
        [TestMethod]
        public void CatPictureService_Returns_LengthNonZero()
        {
            foo = new HttpResponseMessage(HttpStatusCode.OK);
            foo.Content = new StringContent("ACatPicture");
        }

        public async Task Get()
        {
            var mockClient = new Mock<HttpClient>();
            mockClient.Setup(
                client => client.SendAsync(
                    It.IsAny<HttpRequestMessage>(), 
                    It.IsAny<CancellationToken>()
                    )
                ).ReturnsAsync(foo);

            CatPictureService c = new CatPictureService(mockClient);
            await c.GetCatPictureAsync();

            //Stream completedStream = stream.GetAwaiter().GetResult();
            //StreamReader streamReader = new StreamReader(completedStream);

            //Assert.AreNotEqual(streamReader.ReadToEnd().Length, 0);
        }
    }
}s