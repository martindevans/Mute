using System.Net.Http;
using System.Threading.Tasks;

namespace Mute.Moe
{
    public interface IHttpClient
    {
        Task<HttpResponseMessage> GetAsync(string uri);
    }

    public class SimpleHttpClient
        : HttpClient, IHttpClient
    {
    }
}
