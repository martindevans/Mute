using System.Text;
using Discord;
using Discord.WebSocket;
using LlmTornado.Chat;
using LlmTornado.Code;
using Mute.Moe.Tools;
using Serilog;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.ChatFunctions;
using LlmTornado.Common;

namespace Mute.Moe.Services.LLM;


/// <summary>
/// Create <see cref="ChatConversation"/> objects on demand
/// </summary>
public class ChatConversationFactory
{
    private readonly ChatConversationSystemPrompt _prompt;
    private readonly LlmChatModel _model;
    private readonly ToolExecutionEngineFactory _toolFactory;
    private readonly DiscordSocketClient _client;
    private readonly MultiEndpointProvider<LLamaServerEndpoint> _endpoints;

    /// <summary>
    /// Create a new <see cref="ChatConversationFactory"/>
    /// </summary>
    /// <param name="prompt"></param>
    /// <param name="model"></param>
    /// <param name="toolFactory"></param>
    /// <param name="client"></param>
    /// <param name="endpoints"></param>
    public ChatConversationFactory(
        ChatConversationSystemPrompt prompt,
        LlmChatModel model,
        ToolExecutionEngineFactory toolFactory,
        DiscordSocketClient client,
        MultiEndpointProvider<LLamaServerEndpoint> endpoints
    )
    {
        _prompt = prompt;
        _model = model;
        _toolFactory = toolFactory;
        _client = client;
        _endpoints = endpoints;
    }

    /// <summary>
    /// Create a new conversation, bound to the given channel
    /// </summary>
    /// <param name="channel"></param>
    /// <returns></returns>
    public async Task<ChatConversation> Create(IMessageChannel channel)
    {
        // Setup tool execution engine
        var request = new ChatRequest
        {
            Model = _model.Model,
            ParallelToolCalls = true,
        };
        var callCtx = new ITool.CallContext(channel);
        var engine = _toolFactory.GetExecutionEngine(request, callCtx);

        // Sampling parameters
        _model.Sampling?.Apply(request);

        // Build the prompt
        var guild = channel is IDMChannel ? "Direct Message" : ((IGuildChannel)channel).Guild.Name;
        var prompt = FormatPrompt(guild, channel.Name);

        var conv = new ChatConversation(request, _model, engine, _endpoints);
        conv.ReplaceSystemPrompt(prompt);
        return conv;
    }

    private string FormatPrompt(string guild, string channel)
    {
        var promptStr = _prompt.Prompt;

        // Location
        promptStr = promptStr.Replace("{{guild}}", guild);
        promptStr = promptStr.Replace("{{channel}}", channel);

        // Self awareness
        promptStr = promptStr.Replace("{{self_name}}", _client.CurrentUser?.Username ?? "*null");
        promptStr = promptStr.Replace("{{llm_model_name}}", _model.Model.Name);

        return promptStr;
    }
}

/// <summary>
/// An LLM driven chat conversation
/// </summary>
public class ChatConversation
{
    /// <summary>
    /// The model used for this conversation
    /// </summary>
    public ILlmModel Model { get; }

    /// <summary>
    /// The tool execution engine used for this conversation
    /// </summary>
    public ToolExecutionEngine? ToolExecutionEngine { get; }

    private readonly ChatRequest _request;
    private readonly List<ChatMessage> _messages = [ ];
    private readonly MultiEndpointProvider<LLamaServerEndpoint> _endpoints;

    /// <summary>
    /// Total tokens in the conversation state. Only available once a request has been made.
    /// </summary>
    public int? TotalTokens { get; private set; }

    /// <summary>
    /// Total number of messages
    /// </summary>
    public int MessageCount => _messages.Count;

    /// <summary>
    /// Create a new conversation object
    /// </summary>
    /// <param name="request"></param>
    /// <param name="model"></param>
    /// <param name="toolEngine"></param>
    /// <param name="endpoints"></param>
    public ChatConversation(ChatRequest request, ILlmModel model, ToolExecutionEngine? toolEngine, MultiEndpointProvider<LLamaServerEndpoint> endpoints)
    {
        Model = model;
        _request = request;
        ToolExecutionEngine = toolEngine;
        _endpoints = endpoints;
    }

    /// <summary>
    /// Append a user message to the conversation
    /// </summary>
    /// <param name="name"></param>
    /// <param name="message"></param>
    public Guid AddUserMessage(string name, string message)
    {
        _messages.Add(new ChatMessage(ChatMessageRoles.User, $"{name}: {message}")
        {
            Name = name
        });

        return _messages[^1].Id;
    }

    /// <summary>
    /// Append a user message with no name metadata
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public Guid AddAnonymousUserMessage(string message)
    {
        _messages.Add(new ChatMessage(ChatMessageRoles.User, message));

        return _messages[^1].Id;
    }

    /// <summary>
    /// Generate an assistant response and append it to the conversation
    /// </summary>
    /// <returns></returns>
    public async Task GenerateResponse(CancellationToken ct)
    {
        using var endpoint = await _endpoints.GetEndpoint([ Model.Model.Name ],ct);
        if (endpoint == null)
            return;
        var api = endpoint.Endpoint.TornadoApi;

        Log.Information("Chat selected LLM Backend: {0}", endpoint.Endpoint.Url);

        var conversation = api.Chat.CreateConversation(_request);
        conversation.AddMessage(_messages);

        var response = await conversation.GetResponseRich(ExecuteTools, ct);
        TotalTokens = response.Usage?.TotalTokens;

        for (var i = _messages.Count; i < conversation.Messages.Count; i++)
            _messages.Add(conversation.Messages[i]);

        async ValueTask ExecuteTools(List<FunctionCall> calls)
        {
            if (ToolExecutionEngine != null)
            {
                await ToolExecutionEngine.ExecuteValueTask(calls);
            }
            else
            {
                foreach (var call in calls)
                    call.Result = new FunctionResult(call, new { error = "Tools calling is not available" }, false);
            }
        }
    }

    /// <summary>
    /// Keep generating responses until an assistant response is generated.
    /// </summary>
    /// <param name="maxSteps"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async ValueTask<string?> GenerateResponseMultiStep(int maxSteps = 5, CancellationToken ct = default)
    {
        // Generate a response. This takes a long time, since it's making the call to the LLM
        // Keep pumping the system until an assistant response is generated
        string? response = null;
        for (var i = 0; i < maxSteps; i++)
        {
            // Generate a response. This takes a long time, since it's making the call to the LLM
            await GenerateResponse(ct);

            // Extract the response
            response = GetLastAssistantResponse();
            if (response != null)
                break;
        }

        return response;
    }

    /// <summary>
    /// Get the last assistant response from the conversation
    /// </summary>
    /// <returns></returns>
    public string? GetLastAssistantResponse()
    {
        var last = _messages[^1];
        if (last.Role == ChatMessageRoles.Assistant)
            return last.GetMessageContent();

        return null;
    }

    /// <summary>
    /// Remove all messages except for the first
    /// </summary>
    public void Clear(bool removeFirst = false)
    {
        // Remove all messages
        var sys = _messages[0];
        _messages.Clear();

        // Add the first message back in if necessary
        if (!removeFirst)
            _messages.Add(sys);

        // Clear all except for the default tools
        ToolExecutionEngine?.Clear();
    }

    #region Summarisation
    /// <summary>
    /// Summarise the current conversation, clear all messages (except the system prompt) and insert the summary as a message.
    /// </summary>
    /// <returns></returns>
    public async Task<string> AutoSummarise(CancellationToken cancellation)
    {
        const string prompt = """
                              Summarise the conversation so far as a list of bullet points.
                              Each bullet must be a concise factual statement.
                              Include only facts necessary to continue the discussion. Do not store obsolete information.
                              Do not include assistant reasoning, instructions, bookkeeping, negative statements, or meta commentary.
                              When information conflicts retain only the most recent version.
                              Do not restate the assistant's explanations, except where they define constraints, decisions, or agreed approaches.
                              Exclude examples, speculation, repetition, meta commentary, and stylistic language.
                              Use the minimum number of bullet points necessary, with a hard maximum of 10.
                              """;

        // Generate summary
        AddUserMessage("SELF", prompt);
        await GenerateResponse(cancellation);

        // Extract summary
        var summary = GetLastAssistantResponse();

        // Remove everything but the system prompt
        Clear();

        // Insert the summary
        if (summary != null)
            AddUserMessage("SUMMARY", summary);

        // Clear token count, it'll be filled next time a request is made
        TotalTokens = null;

        return summary ?? "";
    }

    /// <summary>
    /// If there is one, find the summary message
    /// </summary>
    /// <returns></returns>
    public string? FindSummaryMessage()
    {
        var summary = _messages
            .Where(a => a.Name == "SUMMARY")
            .Select(a => a.GetMessageContent())
            .FirstOrDefault();
        return summary;
    }
    #endregion

    #region Tool Cleanup
    /// <summary>
    /// Remove tool messages which are "buried" under a certain number of subsequent (non tool) messages
    /// </summary>
    /// <param name="depth"></param>
    /// <returns>Number of messages removed</returns>
    public int CleanBuriedToolMessages(int depth)
    {
        var removed = 0;

        for (var i = _messages.Count - 1; i >= 0; i--)
        {
            var message = _messages[i];

            if (IsToolMessage(message))
            {
                if (depth <= 0)
                {
                    _messages.RemoveAt(i);
                    removed++;
                }
            }
            else
            {
                depth--;
            }
        }

        return removed;
    }

    /// <summary>
    /// Remove tool messages (oldest first) until the total context size is below the target
    /// </summary>
    /// <param name="contextSize"></param>
    public bool SweepToolMessages(int contextSize)
    {
        while (EstimateTokenCount() > contextSize)
        {
            if (!RemoveFirstToolMessage())
                break;
        }

        return EstimateTokenCount() <= contextSize;

        bool RemoveFirstToolMessage()
        {
            for (var i = 0; i < _messages.Count; i++)
            {
                var message = _messages[i];

                if (IsToolMessage(message))
                {
                    _messages.RemoveAt(i);
                    TotalTokens = null;
                    return true;
                }
            }

            return false;
        }
    }

    private static bool IsToolMessage(ChatMessage message)
    {
        return message.Role == ChatMessageRoles.Tool
            || message is { Role: ChatMessageRoles.Assistant, ToolCalls: not null };
    }
    #endregion

    #region save/load
    /// <summary>
    /// Save the messages to a JSON string
    /// </summary>
    /// <returns></returns>
    public string Save()
    {
        var json = JsonSerializer.Serialize(_messages);
        return json;
    }

    /// <summary>
    /// Load messages from the JSON string
    /// </summary>
    /// <param name="json"></param>
    /// <param name="overwriteSystemPrompt"></param>
    public void Load(string json, bool overwriteSystemPrompt = false)
    {
        // Get the system prompt
        var sys = _messages.Count > 0 ? _messages[0] : null;

        // Remove all messages
        _messages.Clear();

        // Load messages
        var messages = JsonSerializer.Deserialize<List<ChatMessage>>(json) ?? [];
        _messages.AddRange(messages);

        // Restore system prompt
        if (sys != null && !overwriteSystemPrompt)
            ReplaceSystemPrompt(sys.GetMessageContent());
    }
    #endregion

    /// <summary>
    /// Get the estimated number of tokens in this conversation
    /// </summary>
    /// <returns></returns>
    public int EstimateTokenCount()
    {
        return TotalTokens ?? _messages.Select(a => a.GetMessageTokens()).Sum();
    }

    /// <summary>
    /// Replace the system prompt message
    /// </summary>
    /// <param name="prompt"></param>
    public void ReplaceSystemPrompt(string prompt)
    {
        if (_messages.Count == 0)
        {
            _messages.Add(new ChatMessage(ChatMessageRoles.System, prompt));
        }
        else
        {
            _messages.RemoveAt(0);
            _messages.Insert(0, new ChatMessage(ChatMessageRoles.System, prompt));
        }
    }

    /// <summary>
    /// Replace a message with a new message, using the same role
    /// </summary>
    /// <param name="id"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    public bool ReplaceMessage(Guid id, string content)
    {
        for (var i = 0; i < _messages.Count; i++)
        {
            if (_messages[i].Id == id)
            {
                _messages[i] = new ChatMessage(
                    _messages[i].Role ?? ChatMessageRoles.User,
                    content
                );
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Extract a transcript of this conversation
    /// </summary>
    /// <param name="assistantName"></param>
    /// <returns></returns>
    public string Transcript(string assistantName)
    {
        var builder = new StringBuilder();

        foreach (var message in _messages)
        {
            switch (message.Role)
            {
                case ChatMessageRoles.User:
                {
                    if (message.Name == null)
                        builder.Append("Anon: ");
                    builder.AppendLine(message.Content);
                    break;
                }

                case ChatMessageRoles.Assistant:
                {
                    builder.Append($"{assistantName}: ");
                    builder.AppendLine(message.GetMessageContent());
                    break;
                }
            }
        }

        return builder.ToString();
    }
}