using System.Collections.Generic;
using System.Threading.Tasks;


namespace Mute.Moe.Services.Information.Wikipedia
{
    public interface IWikipedia
    {
        Task<IReadOnlyList<IDefinition>> Define(string topic, int sentences = 3);
    }

    public interface IDefinition
    {
        string Definition { get; }
        string Title { get; }
        string? Url { get; }
    }
}
