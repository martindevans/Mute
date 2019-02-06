using System.Net.Http;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Mute.Moe.Utilities
{
    public static class IHttpClientExtensions
    {
        public static async Task<HttpResponseMessage> HeadAsync([NotNull] this IHttpClient client, string uri)
        {
            return await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri));
        }
    }

    public interface IHttpClient
    {
        Task<HttpResponseMessage> GetAsync(string uri);

        Task<HttpResponseMessage> SendAsync(HttpRequestMessage message);
    }

    public class SimpleHttpClient
        : HttpClient, IHttpClient
    {
    }
}
