using System.Threading.Tasks;
using Mute.Moe.Tools;

namespace Mute.Moe.Services.Information.Wikipedia;

/// <summary>
/// Rtrieve information from wikipedia
/// </summary>
public interface IWikipedia
{
    /// <summary>
    /// Fetch a definition from wikipedia, limited to a number of sentences
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="sentences"></param>
    /// <returns></returns>
    Task<IReadOnlyList<IDefinition>> Define(string topic, int sentences = 3);
}

/// <summary>
/// A short definition from wikipedia
/// </summary>
public interface IDefinition
{
    /// <summary>
    /// The definition
    /// </summary>
    string Definition { get; }

    /// <summary>
    /// Title of the page
    /// </summary>
    string Title { get; }

    /// <summary>
    /// URL of the page
    /// </summary>
    string? Url { get; }
}

/// <summary>
/// Provide tool access to the wikipedia API
/// </summary>
public class WikipediaToolProvider
    : IToolProvider
{
    private readonly IWikipedia _wiki;

    /// <inheritdoc />
    public IReadOnlyList<ITool> Tools { get; }

    /// <summary>
    /// Construct a new <see cref="WikipediaToolProvider"/>
    /// </summary>
    /// <param name="wiki"></param>
    public WikipediaToolProvider(IWikipedia wiki)
    {
        _wiki = wiki;

        Tools =
        [
            new AutoTool("get_wikipedia_definition", true, GetDefinition)
        ];
    }

    /// <summary>
    /// Get a short definition of a general knowledge topic from wikipedia
    /// </summary>
    /// <param name="term">Search term to define</param>
    /// <returns></returns>
    private async Task<object> GetDefinition(string term)
    {
        var results = await _wiki.Define(term);

        return new
        {
            Results = results
        };
    }
}