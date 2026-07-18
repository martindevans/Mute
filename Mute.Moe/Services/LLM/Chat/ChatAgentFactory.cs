using HandyAgentFramework.Compaction;
using HandyAgentFramework.FunctionCall.Middleware;
using HandyAgentFramework.FunctionCall.Middleware.ToolSearch;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Compaction;
using Microsoft.Extensions.AI;
using Mute.Moe.Services.ImageGen;
using Mute.Moe.Services.LLM.Chat.Middleware;
using System.Threading.Tasks;

namespace Mute.Moe.Services.LLM.Chat;

/// <summary>
/// Create agents for conversational response service
/// </summary>
public class ChatAgentFactory
{
    private readonly AgentChatModel _model;
    private readonly ChatConversationSystemPrompt _prompt;
    private readonly IChatClient _client;
    private readonly IToolSet _tools;
    private readonly IImageAnalyser _analyser;

    /// <summary>
    /// Context size of this agent
    /// </summary>
    public int ContextSize => _model.ContextSize;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatAgentFactory"/> class.
    /// </summary>
    /// <param name="model">The chat model to be used, which defines the context size and other parameters.</param>
    /// <param name="prompt">The system prompt for initializing the chat conversation.</param>
    /// <param name="client">The chat client responsible for handling communication with the backend.</param>
    /// <param name="tools">The set of tools available to agents</param>
    /// <param name="analyser"></param>
    public ChatAgentFactory(AgentChatModel model, ChatConversationSystemPrompt prompt, IChatClient client, IToolSet tools, IImageAnalyser analyser)
    {
        _model = model;
        _prompt = prompt;
        _client = client;
        _tools = tools;
        _analyser = analyser;
    }

    /// <summary>
    /// Create an agent for chat
    /// </summary>
    /// <returns></returns>
    public async Task<AIAgent> Create(IReadOnlyDictionary<string, string> promptTemplateParams)
    {
        var contextSize = _model.ContextSize;
        var maxResponse = _model.ContextSize / 8;

        var prompt = _prompt.Prompt;
        foreach (var (key, value) in promptTemplateParams)
            prompt = prompt.Replace($"{{{{{key}}}}}", value);
        prompt = prompt.Replace("{{llm_model_name}}", _model.Name);

        var options = new ChatOptions
        {
            Instructions = prompt,
            AdditionalProperties = [],
            AllowMultipleToolCalls = true,
            Tools = [],
            MaxOutputTokens = maxResponse,
            ModelId = _model.Name,
        };

#pragma warning disable MAAI001 // (experimental features)
        var reducer = new PipelineCompactionStrategy(
            new EphemeralMessageCompaction(),
            new ToolResultCompactionStrategy(CompactionTriggers.TokensExceed(contextSize / 4)),
            new ContextWindowCompactionStrategy(contextSize, maxResponse),
            new SummarizationCompactionStrategy(_client, CompactionTriggers.TokensExceed(contextSize / 2)),
            new TruncationCompactionStrategy(CompactionTriggers.TokensExceed(contextSize))
#pragma warning restore MAAI001
        ).AsChatReducer();

        var toolSearch = new ToolSearchProvider(_tools);
        
        var context = new AIContextProvider[]
        {
            //todo: SQL files? new FileMemoryProvider(new FileSystemAgentFileStore("memory")),
            new FunctionCallStatisticsMonitor(),
            toolSearch
        };

        var agent = _client
                   .AsAIAgent(
                        new ChatClientAgentOptions
                        {
                            ChatOptions = options,
                            ChatHistoryProvider = new InMemoryChatHistoryProvider(new() { ChatReducer = reducer }),
                            AIContextProviders = context,
                        }).AsBuilder()
                   .Use(toolSearch.OnToolCallMiddleware)
                   .UseToolApproval();

        // Add middleware that converts images in inputs into a description of the image
        if (!_model.IsVisionModel)
        {
            var middleware = new ChatAgentImageAnalysisMiddleware(_analyser);
            agent = agent.Use(middleware.Middleware, middleware.MiddlewareStreaming);
        }

        return agent.Build();
    }
}

