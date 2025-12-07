using LlmTornado.Chat;
using LlmTornado.ChatFunctions;
using LlmTornado.Common;
using LlmTornado.Infra;
using Mute.Moe.Tools.Providers;
using System.Threading.Tasks;

namespace Mute.Moe.Tools;

/// <summary>
/// Creates a <see cref="ToolExecutionEngine"/> bound to a single <see cref="Conversation"/>
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
    /// Create a new <see cref="ToolExecutionEngineFactory"/> for the given conversation
    /// </summary>
    /// <param name="conversation"></param>
    /// <param name="toolSearch"></param>
    /// <param name="defaultTools"></param>
    /// <returns></returns>
    public ToolExecutionEngine GetExecutionEngine(Conversation conversation, bool toolSearch = true, bool defaultTools = true)
    {
        var engine = new ToolExecutionEngine(conversation, _toolsIndex, toolSearch, defaultTools);
        return engine;
    }
}

/// <summary>
/// Executes tools in an LLM conversation. Also provides the `search_for_tools` meta tool
/// </summary>
public class ToolExecutionEngine
{
    private readonly Conversation _conversation;
    private readonly IToolIndex _allTools;
    private readonly Dictionary<string, ITool> _availableTools = [ ];
    private readonly HashSet<string> _bannedTools = [ ];
    private readonly List<string> _calls = [ ];

    /// <summary>
    /// All tools which have been made available to this conversation so far
    /// </summary>
    public IReadOnlyDictionary<string, ITool> AvailableTools => _availableTools;

    /// <summary>
    /// Get a list of all tool calls that have been made
    /// </summary>
    public IReadOnlyList<string> ToolCalls => _calls;

    /// <summary>
    /// Create a new execution engine, adds the `search_for_tools` meta tool and all default tools
    /// </summary>
    /// <param name="conversation"></param>
    /// <param name="tools"></param>
    /// <param name="allowToolSearch"></param>
    /// <param name="addDefaultTools"></param>
    public ToolExecutionEngine(Conversation conversation, IToolIndex tools, bool allowToolSearch, bool addDefaultTools)
    {
        _conversation = conversation;
        _allTools = tools;

        conversation.Update(c =>
        {
            c.Tools ??= [];

            // Add the `search_for_tools` meta tool
            if (allowToolSearch)
            {
                c.Tools.Add(new Tool([
                    new ToolParam("query", "A detailed natural language query describing the functionality of the tool required.", ToolParamAtomicTypes.String)
                ], "search_for_tools", "Performs an approximate/fuzzy search for tools through embedding vector similarity. If no results are found, try rewording your query."));
            }

            // Add default tools
            if (addDefaultTools)
            {
                foreach (var (key, tool) in _allTools.Tools)
                {
                    if (tool.IsDefaultTool)
                    {
                        c.Tools.Add(new Tool(tool.GetParameters().ToList(), tool.Name, tool.Description, strict: true));
                        _availableTools.Add(key, tool);
                    }
                }
            }
        });
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
            _conversation.Update(c =>
            {
                c.Tools ??= [ ];
                c.Tools.Add(new Tool(tool.GetParameters().ToList(), tool.Name, tool.Description, strict: true));
            });
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
        
        _conversation.Update(c =>
        {
            c.Tools ??= [];
            c.Tools.RemoveAll(a => name.Equals(a.Function?.Name));
        });

        return true;

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
    /// Execute a single tool call
    /// </summary>
    /// <param name="functionCall"></param>
    public async Task Execute(FunctionCall functionCall)
    {
        _calls.Add(functionCall.Name);

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
                var (success, result) = await ExecuteTool(functionCall, tool);
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

        // Find all tools, ordered by similarity to query embedding
        var results = (await _allTools.Find(query, limit: 5)).Where(x => !_bannedTools.Contains(x.Tool.Name)).ToList();

        // Add tools
        var matches = new List<string>();
        if (results.Count > 0)
        {
            var top = results[0].Similarity;
            var threshold1 = top * 0.95;
            var threshold2 = 0.8;

            _conversation.Update(c =>
            {
                // Ensure tools list is not null before we add to it
                c.Tools ??= [];

                foreach (var (similarity, tool) in results)
                {
                    // Ignore tools below thresholds
                    if (similarity < threshold1 || similarity < threshold2)
                        continue;

                    // Add to the list of results returned to the LLM
                    matches.Add(tool.Name);

                    // Add new tools to the conversation
                    if (_availableTools.TryAdd(tool.Name, tool))
                        c.Tools.Add(new Tool(tool.GetParameters().ToList(), tool.Name, tool.Description, strict: true));
                }
            });
        }

        return (true, matches);
    }

    private static async Task<(bool success, object result)> ExecuteTool(FunctionCall call, ITool tool)
    {
        try
        {
            return await tool.Execute(call.GetArguments());
        }
        catch (Exception ex)
        {
            return (false, new { error = "An exception was raised while invoking the tool", message = ex.Message });
        }
    }
}