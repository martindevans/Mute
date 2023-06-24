using System.Threading.Tasks;

namespace Mute.Moe.Services.Information.UrbanDictionary;

public interface IUrbanDictionary
{
    Task<IReadOnlyList<IUrbanDefinition>> SearchTermAsync(string term);
}

public interface IUrbanDefinition
{
    string Definition { get; }

    Uri Permalink { get; }

    int ThumbsUp { get; }

    int ThumbsDown { get; }

    string Word { get; }

    DateTime WrittenOn { get; }

    string Example { get; }
}