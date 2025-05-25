using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;

namespace Mute.Moe.Services.Information.RSS;

public class HttpRss
    : IRss
{
    public Task<IEnumerable<SyndicationItem>> Fetch(string url)
    {
        return Task.Run(() =>
        {
            using var reader = XmlReader.Create(url, new()
            {
                DtdProcessing = DtdProcessing.Ignore,
            });

            return SyndicationFeed.Load(reader).Items;
        });
    }
}