using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Mute.Moe.Services.Information.Wikipedia
{
    public interface IWikipedia
    {
        Task<IReadOnlyList<IDefinition>> Define(string topic, int sentences = 3);
    }

    public interface IDefinition
    {
        [NotNull] string Definition { get; }
        [NotNull] string Title { get; }
        [CanBeNull] string Url { get; }
    }
}
