//using System.Threading.Tasks;
//using LlmTornado.Common;
//using LlmTornado.Infra;
//using LlmTornado.Mcp;
//using Serilog;

//namespace Mute.Moe.Tools.Providers;

//public record McpToolProviderConfig(string Name, string Url, string[]? AllowedTools = null, Dictionary<string, string>? AdditionalHeaders = null)
//{
//    public async Task<McpToolProvider?> TryCreateProvider()
//    {
//        var mcp = new MCPServer(
//            serverLabel: Name,
//            serverUrl: Url,
//            allowedTools: null,
//            additionalConnectionHeaders: null
//        );

//        await mcp.InitializeAsync();

//        var client = mcp.McpClient;
//        if (client == null)
//        {
//            Log.Error("Failed to connect to MCP Server: '{0}'@{1}. Tools from this server will be unavailable!", Name, Url);
//            return null;
//        }

//        Log.Information("Connected to MCP Server: '{0}'@{1}", Name, Url);

//        var tools = await client.ListTornadoToolsAsync();

//        return new McpToolProvider(mcp, tools);
//    }
//}

//public class McpToolProvider
//    : IToolProvider
//{
//    private readonly MCPServer _mcp;

//    public McpToolProvider(MCPServer mcp, List<Tool> tools)
//    {
//        _mcp = mcp;
//        Tools = tools.Select(a => new McpToolWrapper(mcp, a)).ToArray();
//    }

//    public IReadOnlyList<ITool> Tools { get; }
//}

//public class McpToolWrapper
//    : ITool
//{
//    private readonly MCPServer _mcp;
//    private readonly Tool _tool;

//    public McpToolWrapper(MCPServer mcp, Tool tool)
//    {
//        _mcp = mcp;
//        _tool = tool;
//    }

//    /// <inheritdoc />
//    public string Name => _tool.ResolvedName;

//    /// <inheritdoc />
//    public string Description => _tool.ResolvedDescription ?? "No description";

//    /// <inheritdoc />
//    public bool IsDefaultTool => false;

//    /// <inheritdoc />
//    public IReadOnlyList<ToolParam> GetParameters()
//    {
//        throw new NotImplementedException();
//    }

//    public Task<(bool success, object result)> Execute(ITool.CallContext context, IReadOnlyDictionary<string, object?> arguments)
//    {
//        throw new NotImplementedException();
//    }
//}