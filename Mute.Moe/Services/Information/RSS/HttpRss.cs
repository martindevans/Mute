using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;

namespace Mute.Moe.Services.Information.RSS;

/// <inheritdoc />
public class HttpRss
    : IRss
{
    /// <inheritdoc />
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