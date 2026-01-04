using System.Text.Json;
using Discord;
using Discord.WebSocket;
using LlmTornado.Chat;
using LlmTornado.Code;
using Mute.Moe.Tools;
using System.Threading;
using System.Threading.Tasks;

namespace Mute.Moe.Services.LLM;

/// <summary>
/// System prompt for LLM chat
/// </summary>
/// <param name="Prompt"></param>
public record ChatConversationSystemPrompt(string Prompt);

/// <summary>
/// Create <see cref="ChatConversation"/> objects on demand
/// </summary>
public class ChatConversationFactory
{
    private readonly ChatConversationSystemPrompt _prompt;
    private readonly ChatModelEndpoint _model;
    private readonly ToolExecutionEngineFactory _toolFactory;
    private readonly DiscordSocketClient _client;

    /// <summary>
    /// Create a new <see cref="ChatConversationFactory"/>
    /// </summary>
    /// <param name="prompt"></param>
    /// <param name="model"></param>
    /// <param name="toolFactory"></param>
    /// <param name="client"></param>
    public ChatConversationFactory(ChatConversationSystemPrompt prompt, ChatModelEndpoint model, ToolExecutionEngineFactory toolFactory, DiscordSocketClient client)
    {
        _prompt = prompt;
        _model = model;
        _toolFactory = toolFactory;
        _client = client;
    }

    /// <summary>
    /// Create a new conversation, bound to the given channel
    /// </summary>
    /// <param name="channel"></param>
    /// <returns></returns>
    public async Task<ChatConversation> Create(IMessageChannel channel)
    {
        // Build the prompt
        var guild = channel is IDMChannel ? "Direct Message" : ((IGuildChannel)channel).Guild.Name;
        var prompt = FormatPrompt(guild, channel.Name);

        // Create conversation object
        var conversation = _model.Api.Chat.CreateConversation(new ChatRequest
        {
            Model = _model.Model,
            ParallelToolCalls = true,
        });
        conversation.AddSystemMessage(prompt);

        // Setup tool execution engine
        var callCtx = new ITool.CallContext(channel);
        var engine = _toolFactory.GetExecutionEngine(conversation, callCtx);

        return new ChatConversation(conversation, _model, engine);
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
    public ChatModelEndpoint Model { get; }

    /// <summary>
    /// The tool execution engine used for this conversation
    /// </summary>
    public ToolExecutionEngine ToolExecutionEngine { get; }

    private readonly Conversation _conversation;
    
    /// <summary>
    /// Total tokens in the conversation state. Only available once a request has been made.
    /// </summary>
    public int? TotalTokens { get; private set; }

    /// <summary>
    /// Total number of messages
    /// </summary>
    public int MessageCount => _conversation.Messages.Count;

    /// <summary>
    /// Create a new conversation object
    /// </summary>
    /// <param name="conversation"></param>
    /// <param name="model"></param>
    /// <param name="toolEngine"></param>
    public ChatConversation(Conversation conversation, ChatModelEndpoint model, ToolExecutionEngine toolEngine)
    {
        Model = model;
        _conversation = conversation;
        ToolExecutionEngine = toolEngine;
    }

    /// <summary>
    /// Append a user message to the conversation
    /// </summary>
    /// <param name="name"></param>
    /// <param name="message"></param>
    public Guid AddUserMessage(string name, string message)
    {
        _conversation.AddMessage(new ChatMessage(ChatMessageRoles.User, $"[{name}]: {message}")
        {
            Name = name
        });

        return _conversation.Messages[^1].Id;
    }

    /// <summary>
    /// Generate an assistant response and append it to the conversation
    /// </summary>
    /// <returns></returns>
    public async Task GenerateResponse(CancellationToken ct)
    {
        var response = await _conversation.GetResponseRich(ToolExecutionEngine.ExecuteValueTask, ct);
        TotalTokens = response.Usage?.TotalTokens;
    }

    /// <summary>
    /// Get the last assistant response from the conversation
    /// </summary>
    /// <returns></returns>
    public string? GetLastAssistantResponse()
    {
        var last = _conversation.Messages[^1];
        if (last.Role == ChatMessageRoles.Assistant)
            return last.GetMessageContent();

        return null;
    }

    /// <summary>
    /// Remove all messages except for the first
    /// </summary>
    public void Clear(bool removeFirst = false)
    {
        var sys = _conversation.Messages[0];
        _conversation.Clear();

        if (!removeFirst)
            _conversation.AddMessage(sys);
    }

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

        return summary ?? "";
    }

    /// <summary>
    /// If there is one, find the summary message
    /// </summary>
    /// <returns></returns>
    public string? FindSummaryMessage()
    {
        var summary = _conversation.Messages.Where(a => a.Name == "SUMMARY").Select(a => a.GetMessageContent()).FirstOrDefault();
        return summary;
    }

    /// <summary>
    /// Remove tool messages which are "buried" under a certain number of subsequent (non tool) messages
    /// </summary>
    /// <param name="depth"></param>
    /// <returns>Number of messages removed</returns>
    public int CleanToolMessages(int depth)
    {
        var removed = 0;

        for (var i = _conversation.Messages.Count - 1; i >= 0; i--)
        {
            var message = _conversation.Messages[i];

            switch (message.Role)
            {
                case ChatMessageRoles.Assistant when message.ToolCalls != null:
                case ChatMessageRoles.Tool:
                {
                    if (depth <= 0)
                    {
                        _conversation.RemoveMessage(message);
                        removed++;
                    }

                    break;
                }

                case ChatMessageRoles.User or ChatMessageRoles.Assistant:
                    depth--;
                    break;
            }
        }

        return removed;
    }

    /// <summary>
    /// Save the messages to a JSON string
    /// </summary>
    /// <returns></returns>
    public string Save()
    {
        var json = JsonSerializer.Serialize(_conversation.Messages);
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
        var sys = _conversation.Messages.Count > 0 ? _conversation.Messages[0] : null;

        // Remove all messages
        _conversation.Clear();

        // Load messages
        var messages = JsonSerializer.Deserialize<List<ChatMessage>>(json) ?? [];
        _conversation.AddMessage(messages);

        // Restore system prompt
        if (sys != null && !overwriteSystemPrompt)
            _conversation.SetSystemMessage(sys.GetMessageContent());
    }

    /// <summary>
    /// Get the estimated number of tokens in this conversation
    /// </summary>
    /// <returns></returns>
    public int EstimateTokenCount()
    {
        return TotalTokens ?? _conversation.Messages.Select(a => a.GetMessageTokens()).Sum();
    }
}