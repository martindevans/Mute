using System.Threading.Tasks;

namespace Mute.Moe.Services.Information.UrbanDictionary;

/// <summary>
/// Search urban dictionary
/// </summary>
public interface IUrbanDictionary
{
    /// <summary>
    /// Search urban dictionary
    /// </summary>
    /// <param name="term"></param>
    /// <returns></returns>
    Task<IReadOnlyList<IUrbanDefinition>> SearchTermAsync(string term);
}

/// <summary>
/// A definition from irban dictionary
/// </summary>
public interface IUrbanDefinition
{
    /// <summary>
    /// The definition
    /// </summary>
    string Definition { get; }

    /// <summary>
    /// Permalink for this definition
    /// </summary>
    Uri Permalink { get; }

    /// <summary>
    /// Number of thumbs up votes
    /// </summary>
    int ThumbsUp { get; }

    /// <summary>
    /// Number of thumbs down votes
    /// </summary>
    int ThumbsDown { get; }

    /// <summary>
    /// The word being defined
    /// </summary>
    string Word { get; }

    /// <summary>
    /// Date this definition was written on
    /// </summary>
    DateTime WrittenOn { get; }

    /// <summary>
    /// An example of this word being used
    /// </summary>
    string Example { get; }
}