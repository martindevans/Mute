using System.IO;
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

        // Time
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/London"));
        promptStr = promptStr.Replace("{{localTime}}", localTime.ToShortTimeString());
        promptStr = promptStr.Replace("{{localDate}}", localTime.ToShortDateString());

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
    public ChatModelEndpoint Model { get; }
    public Conversation Conversation { get; }
    public ToolExecutionEngine ToolExecutionEngine { get; }

    /// <summary>
    /// Total tokens in the conversation state
    /// </summary>
    public int? TotalTokens { get; private set; }

    /// <summary>
    /// Total number of messages
    /// </summary>
    public int MessageCount => Conversation.Messages.Count;

    public ChatConversation(Conversation conversation, ChatModelEndpoint model, ToolExecutionEngine toolEngine)
    {
        Model = model;
        Conversation = conversation;
        ToolExecutionEngine = toolEngine;
    }

    /// <summary>
    /// Append a user message to the conversation
    /// </summary>
    /// <param name="name"></param>
    /// <param name="message"></param>
    public Guid AddUserMessage(string name, string message)
    {
        Conversation.AddUserMessage($"[{name}]: {message}");

        return Conversation.Messages[^1].Id;
    }

    /// <summary>
    /// Generate an assistant response and append it to the conversation
    /// </summary>
    /// <returns></returns>
    public async Task GenerateResponse(CancellationToken ct)
    {
        var response = await Conversation.GetResponseRich(ToolExecutionEngine.ExecuteValueTask, ct);
        TotalTokens = response.Usage?.TotalTokens;
    }

    /// <summary>
    /// Get the last assistant response from the conversation
    /// </summary>
    /// <returns></returns>
    public string? GetLastAssistantResponse()
    {
        var last = Conversation.Messages[^1];
        if (last.Role == ChatMessageRoles.Assistant)
            return last.GetMessageContent();

        return null;
    }

    /// <summary>
    /// Remove all messages except for the first
    /// </summary>
    public void Clear(bool removeFirst = false)
    {
        var sys = Conversation.Messages[0];
        Conversation.Clear();

        if (!removeFirst)
            Conversation.AddMessage(sys);
    }

    /// <summary>
    /// Summarise the current conversation, clear all messages (except the system prompt) and insert the summary as a message.
    /// </summary>
    /// <returns></returns>
    public async Task<string> Summarise(CancellationToken cancellation)
    {
        // Generate summary
        AddUserMessage(
            "SELF",
            "Summarise the conversation so far as a bullet point list. No more than 10 items, preferably fewer. Discard all information which is not necessary to continue the current conversation topic."
        );
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
    /// Remove tool messages which are "buried" until a certain number of subsequent (non tool) messages
    /// </summary>
    /// <param name="depth"></param>
    /// <returns>Number of messages removed</returns>
    public int CleanToolMessages(int depth)
    {
        var removed = 0;

        for (var i = Conversation.Messages.Count - 1; i >= 0; i--)
        {
            var message = Conversation.Messages[i];

            switch (message.Role)
            {
                case ChatMessageRoles.Assistant when message.ToolCalls != null:
                case ChatMessageRoles.Tool:
                {
                    if (depth <= 0)
                    {
                        Conversation.RemoveMessage(message);
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
}