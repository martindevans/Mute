using System.Net.Http;
using System.Threading.Tasks;

namespace Mute
{
    public interface IHttpClient
    {
        Task<HttpResponseMessage> GetAsync(string uri);
    }
}
