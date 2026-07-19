using System.Threading;
using System.Threading.Tasks;
using HandyAgentFramework;
using HandyAgentFramework.FunctionCall.Middleware.ToolSearch;

namespace Mute.Moe.Tools.Index;

/// <summary>
/// A tool set with no tools
/// </summary>
public class NullToolSet
    : IToolSet
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<IToolSet.SearchResult>> Search(string query, int? topK = null, CancellationToken cancellation = default)
    {
        return [ ];
    }

    /// <inheritdoc />
    public ToolDefinition? TryGetTool(string name)
    {
        return null;
    }

    /// <inheritdoc />
    public IEnumerable<ToolDefinition> DefaultTools()
    {
        return [ ];
    }

    /// <inheritdoc />
    public IEnumerable<ToolDefinition> Tools()
    {
        return [ ];
    }

    /// <inheritdoc />
    public IEnumerable<ToolDefinition> GetToolGroup(string group)
    {
        return [ ];
    }
}