namespace Mute.Moe.Tools.Providers;

/// <summary>
/// Provides a set of tools for LLM usage
/// </summary>
public interface IToolProvider
{
    /// <summary>
    /// All tools provided by this provider
    /// </summary>
    IReadOnlyList<ITool> Tools { get; }
}