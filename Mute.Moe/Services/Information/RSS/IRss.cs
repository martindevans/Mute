using System.ServiceModel.Syndication;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Information.RSS;

/// <summary>
/// Fetches RSS
/// </summary>
public interface IRss
{
    /// <summary>
    /// Fetch and RSS feed
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    Task<IEnumerable<SyndicationItem>> Fetch(string url);
}