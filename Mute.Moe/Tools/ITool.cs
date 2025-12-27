using System.Threading.Tasks;
using Discord;
using LlmTornado.Infra;

namespace Mute.Moe.Tools;

/// <summary>
/// A tool for a language model
/// </summary>
public interface ITool
{
    /// <summary>
    /// Name of this tool
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Short description of the functionality of this tool
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Indicates if this tool should be added to all contexts
    /// </summary>
    bool IsDefaultTool { get; }

    /// <summary>
    /// Get the parameters which must be passed to this tool
    /// </summary>
    /// <returns></returns>
    IReadOnlyList<ToolParam> GetParameters();

    /// <summary>
    /// Try to execute this tool with the given parameters
    /// </summary>
    /// <param name="context">Context of where this tool call is being made</param>
    /// <param name="arguments">Arguments from the model</param>
    /// <returns></returns>
    Task<(bool success, object result)> Execute(CallContext context, IReadOnlyDictionary<string, object?> arguments);

    /// <summary>
    /// Context info about where a tool call was made
    /// </summary>
    public record CallContext(IMessageChannel Channel);
}