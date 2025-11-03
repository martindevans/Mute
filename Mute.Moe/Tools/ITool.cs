using System.Threading.Tasks;
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
    /// <param name="arguments"></param>
    /// <returns></returns>
    Task<(bool success, object result)> Execute(IReadOnlyDictionary<string, object?> arguments);
}