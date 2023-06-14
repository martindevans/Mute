using System.ServiceModel.Syndication;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Information.RSS;

public interface IRss
{
    Task<IEnumerable<SyndicationItem>> Fetch(string url);
}