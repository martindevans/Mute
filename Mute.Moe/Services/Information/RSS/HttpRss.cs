using System.Collections.Generic;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;

namespace Mute.Moe.Services.Information.RSS
{
    public class HttpRss
        : IRss
    {
        public Task<IEnumerable<SyndicationItem>> Fetch(string url)
        {
            return Task.Run(() => {

                var reader = XmlReader.Create(url);
                var feed = SyndicationFeed.Load(reader);
                reader.Close();

                return feed.Items;
            });
        }
    }
}
