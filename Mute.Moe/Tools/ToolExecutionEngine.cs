using LlmTornado.Chat;
using LlmTornado.ChatFunctions;
using LlmTornado.Common;
using LlmTornado.Infra;
using Mute.Moe.Tools.Providers;
using System.Threading.Tasks;
using Serilog;

namespace Mute.Moe.Tools;

/// <summary>
/// Creates a <see cref="ToolExecutionEngine"/> bound to a single <see cref="ChatRequest"/>
/// </summary>
public class ToolExecutionEngineFactory
{
    private readonly IToolIndex _toolsIndex;

    /// <summary>
    /// All tool providers available
    /// </summary>
    public IReadOnlyList<IToolProvider> Providers => _toolsIndex.Providers;

    /// <summary>
    /// All tools from all providers
    /// </summary>
    public IReadOnlyDictionary<string, ITool> Tools => _toolsIndex.Tools;

    /// <summary>
    /// Create a new factory that can produces <see cref="ToolExecutionEngine"/>s
    /// </summary>
    /// <param name="toolsIndex"></param>
    public ToolExecutionEngineFactory(IToolIndex toolsIndex)
    {
        _toolsIndex = toolsIndex;
    }

    /// <summary>
    /// Create a new <see cref="ToolExecutionEngineFactory"/> for the given chat request parameters
    /// </summary>
    /// <param name="requestParameters"></param>
    /// <param name="context"></param>
    /// <param name="toolSearch"></param>
    /// <param name="defaultTools"></param>
    /// <returns></returns>
    public ToolExecutionEngine GetExecutionEngine(ChatRequest requestParameters, ITool.CallContext context, bool toolSearch = true, bool defaultTools = true)
    {
        var engine = new ToolExecutionEngine(requestParameters, _toolsIndex, toolSearch, defaultTools, context);
        return engine;
    }
}

/// <summary>
/// Executes tools in an LLM conversation. Also provides the `search_for_tools` meta tool
/// </summary>
public class ToolExecutionEngine
{
    private readonly ChatRequest _requestParameters;
    private readonly IToolIndex _allTools;
    private readonly ITool.CallContext _context;
    private readonly Dictionary<string, ITool> _availableTools = [ ];
    private readonly HashSet<string> _bannedTools = [ ];
    private readonly List<(string Name, string Json)> _calls = [ ];

    private readonly bool _allowToolSearch;
    private readonly bool _addDefaultTools;

    /// <summary>
    /// All tools which have been made available to this conversation so far
    /// </summary>
    public IReadOnlyDictionary<string, ITool> AvailableTools => _availableTools;

    /// <summary>
    /// Get a list of all tool calls that have been made
    /// </summary>
    public IReadOnlyList<(string Name, string Json)> ToolCalls => _calls;

    /// <summary>
    /// Create a new execution engine, adds the `search_for_tools` meta tool and all default tools
    /// </summary>
    /// <param name="requestParameters"></param>
    /// <param name="tools"></param>
    /// <param name="allowToolSearch"></param>
    /// <param name="addDefaultTools"></param>
    /// <param name="context"></param>
    public ToolExecutionEngine(ChatRequest requestParameters, IToolIndex tools, bool allowToolSearch, bool addDefaultTools, ITool.CallContext context)
    {
        _requestParameters = requestParameters;
        _allTools = tools;
        _context = context;

        // Add the initial toolset
        _allowToolSearch = allowToolSearch;
        _addDefaultTools = addDefaultTools;
        InitialiseTools();
    }

    private void InitialiseTools()
    {
        _requestParameters.Tools ??= [];

        // Add the `search_for_tools` meta tool
        if (_allowToolSearch)
        {
            _requestParameters.Tools.Add(new Tool([
                new ToolParam(
                    "query",
                    """
                    Describe the abstract capabilities required from a tool to satisfy the user’s request.

                    Should be a short description including the following fields:
                    - Capability
                    - Inputs
                    - Outputs

                    Rules:
                    - Describe general functionality, not a specific task instance
                    - Do NOT include proper nouns, dates, times, quantities, or user-specific details
                    - Focus on what the tool can do, as if describing an API or function signature
                    - Do NOT phrase the output as a question or instruction
                    """,
                    ToolParamAtomicTypes.String
                )
            ], "search_for_tools", "Performs an approximate/fuzzy search for tools through vector embeddings. If no results are found, try rewording your query."));
        }

        // Add default tools
        if (_addDefaultTools)
        {
            foreach (var (key, tool) in _allTools.Tools)
            {
                if (tool.IsDefaultTool && !_bannedTools.Contains(key))
                {
                    _requestParameters.Tools.Add(new Tool(tool.GetParameters().ToList(), tool.Name, tool.Description, strict: true));
                    _availableTools.Add(key, tool);
                }
            }
        }
    }

    /// <summary>
    /// Add a specific tool to the chat context.
    /// </summary>
    /// <param name="name"></param>
    /// <returns>True, if the tool was found in the index</returns>
    public async Task<bool> AddTool(string name)
    {
        // Try to get the tool from the index
        if (!_allTools.Tools.TryGetValue(name, out var tool))
            return false;

        // Add it to the conversation
        if (_availableTools.TryAdd(tool.Name, tool))
        {
            _requestParameters.Tools ??= [ ];
            _requestParameters.Tools.Add(new Tool(tool.GetParameters().ToList(), tool.Name, tool.Description, strict: true));
        }

        return true;
    }

    /// <summary>
    /// Ban a specific tool from this chat context. If the tool has been added already it will be removed.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public async Task<bool> BanTool(string name)
    {
        _bannedTools.Add(name);

        if (!_availableTools.ContainsKey(name))
            return false;
        
        _requestParameters.Tools ??= [];
        _requestParameters.Tools.RemoveAll(a => name.Equals(a.Function?.Name));

        return true;

    }

    /// <summary>
    /// Remove all tools except for the basics
    /// </summary>
    public void Clear()
    {
        // Remove all tools from parameters
        _requestParameters.Tools = [ ];

        // Clear the associated set of available tools
        _availableTools.Clear();

        // Re-add the basic tools
        InitialiseTools();
    }

    /// <summary>
    /// Execute all tool calls in list
    /// </summary>
    /// <param name="calls"></param>
    public async Task Execute(List<FunctionCall> calls)
    {
        foreach (var functionCall in calls)
            await Execute(functionCall);
    }

    /// <summary>
    /// Execute all tool calls in list, returns a value task
    /// </summary>
    /// <param name="calls"></param>
    public async ValueTask ExecuteValueTask(List<FunctionCall> calls)
    {
        await Execute(calls);
    }

    /// <summary>
    /// Execute a single tool call
    /// </summary>
    /// <param name="functionCall"></param>
    public async Task Execute(FunctionCall functionCall)
    {
        _calls.Add((functionCall.Name, functionCall.GetJson()));

        if (_bannedTools.Contains(functionCall.Name))
        {
            var error = new
            {
                error = $"Tool '{functionCall.Name}' is not allowed in this context"
            };

            functionCall.Result = new FunctionResult(functionCall, error, false);
            return;
        }

        switch (functionCall.Name)
        {
            // Meta tool to search for tools
            case "search_for_tools":
            {
                var (success, result) = await SearchForTools(functionCall);
                functionCall.Result = new FunctionResult(functionCall, result, success);
                break;
            }

            // Tool is available
            case var key when _availableTools.TryGetValue(key, out var tool):
            {
                var (success, result) = await ExecuteTool(functionCall, tool, _context);
                functionCall.Result = new FunctionResult(functionCall, result, success);
                break;
            }

            //// Tool exists, but is not available
            //case var key when _allTools.ContainsKey(key):
            //{
            //    functionCall.Result = new FunctionResult(
            //        functionCall,
            //        new { error = $"Must call 'get_tool_info' to get detailed tool documentation before attempting to use tool '{key}'" },
            //        false
            //    );
            //    break;
            //}

            case var unknown:
            {
                functionCall.Result = new FunctionResult(
                    functionCall,
                    new { error = $"Unknown tool '{unknown}'" },
                    false
                );
                break;
            }
        }
    }

    /// <summary>
    /// Execute a handler for the `search_for_tools` function
    /// </summary>
    /// <param name="call"></param>
    /// <returns></returns>
    private async Task<(bool success, object result)> SearchForTools(FunctionCall call)
    {
        // Get the 'query' parameter
        if (!call.GetArguments().GetToolParameterString("query", out var query, out var error))
            return (false, error);

        Log.Information("Tool search: {0}", query);

        // Find all tools, ordered by similarity to query embedding
        var results = (await _allTools.Find(query, limit: 5)).Where(x => !_bannedTools.Contains(x.Tool.Name)).ToList();

        // Add tools
        var matches = new List<string>();
        if (results.Count > 0)
        {
            var top = results[0].Relevance;
            var threshold1 = top * 0.9;

            // Ensure tools list is not null before we add to it
            _requestParameters.Tools ??= [];

            foreach (var (similarity, tool) in results)
            {
                // Ignore tools below thresholds
                if (similarity < threshold1)
                    continue;

                // Add to the list of results returned to the LLM
                matches.Add(tool.Name);

                // Add new tools to the conversation
                if (_availableTools.TryAdd(tool.Name, tool))
                    _requestParameters.Tools.Add(new Tool(tool.GetParameters().ToList(), tool.Name, tool.Description, strict: true));
            }
        }

        Log.Information("Tool search results: [{0}]", string.Join(", ", matches));

        return (true, matches);
    }

    private static async Task<(bool success, object result)> ExecuteTool(FunctionCall call, ITool tool, ITool.CallContext context)
    {
        try
        {
            return await tool.Execute(context, call.GetArguments());
        }
        catch (Exception ex)
        {
            return (false, new { error = "An exception was raised while invoking the tool", message = ex.Message });
        }
    }
}