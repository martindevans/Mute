using Discord;
using Discord.WebSocket;
using Mute.Moe.Discord.Context;
using Mute.Moe.Services.LLM;
using Serilog;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Mute.Moe.Discord.Services.Responses;

/// <summary>
/// A chat conversation that uses LLMs to respond
/// </summary>
public class LlmChatConversation
{
    /// <summary>
    /// The channel this conversation is bound to
    /// </summary>
    public ISocketMessageChannel Channel { get; }

    private readonly CancellationTokenSource _stopper = new();
    private readonly Channel<MessageContext> _messages = System.Threading.Channels.Channel.CreateUnbounded<MessageContext>();
    private readonly string _username;

    private readonly IConversationStateStorage _chatStorage;

    /// <summary>
    /// Indicates if this conversation is complete and should not be used again
    /// </summary>
    public bool IsComplete { get; private set; }

    /// <summary>
    /// The last time this conversation was updated
    /// </summary>
    public DateTime LastUpdated { get; private set; }

    /// <summary>
    /// Number of messages waiting for processing
    /// </summary>
    public int QueueCount => _messages.Reader.Count;

    /// <summary>
    /// The current state of the processing queue for this conversation
    /// </summary>
    public ProcessingState State { get; private set; }

    /// <summary>
    /// The last summary that was generated for this conversation
    /// </summary>
    public string? Summary { get; private set; }

    /// <summary>
    /// Get the current number of messages in this conversation
    /// </summary>
    public int MessageCount { get; private set; }

    /// <summary>
    /// Get how much of the context is currently used (0 to 1)
    /// </summary>
    public float ContextUsage { get; private set; }

    /// <summary>
    /// Create a new <see cref="LlmChatConversation"/> for the given channel.
    /// </summary>
    /// <param name="conversation"></param>
    /// <param name="channel"></param>
    /// <param name="client"></param>
    /// <param name="chatStorage"></param>
    public LlmChatConversation(ChatConversation conversation, ISocketMessageChannel channel, DiscordSocketClient client, IConversationStateStorage chatStorage)
    {
        Channel = channel;    

        _username = $"@{client.CurrentUser.Username}";
        _chatStorage = chatStorage;

        Task.Run(async () => await MessageConsumer(conversation));
    }

    /// <summary>
    /// Stop the processing loop for this conversation
    /// </summary>
    /// <returns></returns>
    public async Task Stop()
    {
        _messages.Writer.TryComplete();
        await _stopper.CancelAsync();
    }

    private async Task MessageConsumer(ChatConversation conversation)
    {
        // There are three compression thresholds. Compress if:
        // - There's no work and the conversation is a bit full
        // - There's some work but the conversation is quite full
        // - The conversation is nearly overflowing
        var contextTokens = conversation.Model.Model.ContextTokens ?? 4096;
        var LowCompressThreshold = (int)MathF.Floor(contextTokens * 0.5f);
        var MidCompressThreshold = (int)MathF.Floor(contextTokens * 0.7f);
        var HighCompressThreshold = (int)MathF.Floor(contextTokens * 0.9f);

        try
        {
            State = ProcessingState.Saving;

            // Try to load conversation state
            var state = await _chatStorage.Get(Channel.Id);
            if (state != null)
                conversation.Load(state.Json);

            State = ProcessingState.WaitingForMessage;
            await foreach (var context in _messages.Reader.ReadAllAsync(_stopper.Token))
            {
                // Update stats
                MessageCount = conversation.MessageCount;
                ContextUsage = (conversation.TotalTokens ?? 0) / (float)contextTokens;

                State = ProcessingState.GeneratingResponse;

                using (Channel.EnterTypingState())
                {
                    // Add incoming message to conversation
                    conversation.AddUserMessage(context.User.GlobalName, context.Content);

                    // Generate a response. This takes a long time, since it's making the call to the LLM
                    // Keep pumping the system until an assistant response is generated
                    string? response = null;
                    for (var i = 0; i < 5; i++)
                    {
                        // Generate a response. This takes a long time, since it's making the call to the LLM
                        await conversation.GenerateResponse(_stopper.Token);

                        // Extract the response
                        response = conversation.GetLastAssistantResponse();
                        if (response != null)
                            break;
                    }

                    // Post it to the relevant channel
                    if (!string.IsNullOrEmpty(response))
                        await Channel.SendLongMessageAsync(response);
                }

                State = ProcessingState.Summarising;

                // Clean up "buried" tool messages
                var removedTools = conversation.CleanToolMessages(3);
                if (removedTools > 0)
                    Log.Information("Cleaned up {2} tool messages from conversation in channel {0} ({1})", Channel.Name, Channel.Id, removedTools);

                // Summarise the conversation if there's no work pending
                if (conversation.TotalTokens > LowCompressThreshold && _messages.Reader.Count == 0)
                    Summary = await conversation.Summarise(_stopper.Token);

                // Summarise the conversation even if there's work pending
                else if (conversation.TotalTokens > MidCompressThreshold)
                    Summary = await conversation.Summarise(_stopper.Token);

                // Summarisation failed, just clear the state.
                if (conversation.TotalTokens > HighCompressThreshold)
                {
                    Log.Warning("Compression failed for conversation state in channel {0} ({1})", Channel.Name, Channel.Id);
                    conversation.Clear();
                    Summary = null;
                }

                State = ProcessingState.Saving;

                // Store state in DB
                await _chatStorage.Put(Channel.Id, new ConversationStateData(conversation.Save()));

                State = ProcessingState.WaitingForMessage;
            }
        }
        catch (OperationCanceledException)
        {
            await _chatStorage.Put(Channel.Id, new ConversationStateData(conversation.Save()));
            throw;
        }
        catch (Exception ex)
        {
            Log.Error("Exception killed conversation processor for channel {0} ({1}): {2}", Channel.Name, Channel.Id, ex);
        }
        finally
        {
            IsComplete = true;
        }
    }

    /// <summary>
    /// Enqueue a message to be added to the conversation
    /// </summary>
    /// <param name="context"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task Enqueue(MuteCommandContext context, CancellationToken ct = default)
    {
        // Update the last used time
        LastUpdated = DateTime.UtcNow;

        // Strip bot mention from start of string
        var content = context
            .Message
            .Resolve(userHandling: TagHandling.Name)
            .Trim()
            .TrimStart(_username)
            .TrimStart();

        // Enqueue work
        await _messages.Writer.WriteAsync(new(context.User, new string(content)), ct);
    }

    private record struct MessageContext(IUser User, string Content);

    /// <summary>
    /// Current state of the processing task for this conversation
    /// </summary>
    public enum ProcessingState
    {
        /// <summary>
        /// Waiting for a message to process
        /// </summary>
        WaitingForMessage,

        /// <summary>
        /// Generating an response using the LLM
        /// </summary>
        GeneratingResponse,

        /// <summary>
        /// Running context summarisation
        /// </summary>
        Summarising,

        /// <summary>
        /// Interacting with persistent storage
        /// </summary>
        Saving,
    }
}