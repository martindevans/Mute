using HandyAgentFramework.Compaction;
using HandyAgentFramework.FunctionCall.Middleware;
using HandyAgentFramework.FunctionCall.Middleware.ToolSearch;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Compaction;
using Microsoft.Extensions.AI;
using Mute.Moe.Services.ImageGen;
using Mute.Moe.Services.LLM.Chat.Middleware;
using System.Threading.Tasks;
using Discord;
using Mute.Moe.Discord.Services.Users;

namespace Mute.Moe.Services.LLM.Chat;

/// <summary>
/// Create agents for conversational response service
/// </summary>
public class ChatAgentFactory
{
    private readonly AgentChatModel _chatModel;
    private readonly AgentSummaryModel _summaryModel;
    private readonly ChatConversationSystemPrompt _prompt;
    private readonly IChatClient _client;
    private readonly IToolSet _tools;
    private readonly IImageAnalyser _analyser;
    private readonly IDiscordClient _discordClient;
    private readonly IUserService _users;

    /// <summary>
    /// Context size of this agent
    /// </summary>
    public int ContextSize => _chatModel.ContextSize;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatAgentFactory"/> class.
    /// </summary>
    /// <param name="chatModel">The chat model to be used, which defines the context size and other parameters.</param>
    /// <param name="summaryModel"></param>
    /// <param name="prompt">The system prompt for initializing the chat conversation.</param>
    /// <param name="client">The chat client responsible for handling communication with the backend.</param>
    /// <param name="tools">The set of tools available to agents</param>
    /// <param name="analyser"></param>
    /// <param name="discordClient"></param>
    /// <param name="users"></param>
    public ChatAgentFactory(
        AgentChatModel chatModel,
        AgentSummaryModel summaryModel,
        ChatConversationSystemPrompt prompt,
        IChatClient client,
        IToolSet tools,
        IImageAnalyser analyser,
        IDiscordClient discordClient,
        IUserService users
    )
    {
        _chatModel = chatModel;
        _summaryModel = summaryModel;
        _prompt = prompt;
        _client = client;
        _tools = tools;
        _analyser = analyser;
        _discordClient = discordClient;
        _users = users;
    }

    /// <summary>
    /// Create an agent for chat
    /// </summary>
    /// <returns></returns>
    public async Task<AIAgent> Create(IReadOnlyDictionary<string, string> promptTemplateParams)
    {
        var contextSize = _chatModel.ContextSize;
        var maxResponse = _chatModel.ContextSize / 8;

        var prompt = _prompt.Prompt;
        foreach (var (key, value) in promptTemplateParams)
            prompt = prompt.Replace($"{{{{{key}}}}}", value);
        prompt = prompt.Replace("{{llm_model_name}}", _chatModel.Name);

        var options = new ChatOptions
        {
            Instructions = prompt,
            AdditionalProperties = [],
            AllowMultipleToolCalls = true,
            Tools = [],
            MaxOutputTokens = maxResponse,
            ModelId = _chatModel.Name,
        };

        var summaryClient = _client
            .AsBuilder()
            .ConfigureOptions(options =>
            {
                options.ModelId ??= _summaryModel.Name;
            })
            .Build();

#pragma warning disable MAAI001 // (experimental features)
        var threshold1 = CompactionTriggers.TokensExceed((int)(contextSize * 0.40f));
        var threshold2 = CompactionTriggers.TokensExceed((int)(contextSize * 0.70f));
        var threshold3 = CompactionTriggers.TokensExceed((int)(contextSize * 0.85f));

        var reducer = new PipelineCompactionStrategy(
            new EphemeralMessageCompaction(),

            // 1. Gentle: collapse old tool-call groups into short summaries
            new ToolResultCompactionStrategy(threshold1, minimumPreservedGroups:8),

            // 2. Moderate: use an LLM to summarize older conversation spans into a concise message
            new SummarizationCompactionStrategy(summaryClient, threshold2),

            // 4. Emergency: drop oldest groups until under the token budget
            new TruncationCompactionStrategy(threshold3)
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

        // Add middleware that prepends name to message. Do this before vision middleware!
        var prepend = new PrependMessageMetadata(_discordClient, _users);
        agent = agent.Use(prepend.Middleware, prepend.MiddlewareStreaming);

        // Add middleware that converts images in inputs into a description of the image
        if (!_chatModel.IsVisionModel)
        {
            var middleware = new AgentAgentImageAnalysisMiddleware(_analyser);
            agent = agent.Use(middleware.Middleware, middleware.MiddlewareStreaming);
        }

        return agent.Build();
    }
}

