using Discord;
using Discord.WebSocket;
using Mute.Moe.Discord.Context;
using Mute.Moe.Services.LLM;
using Mute.Moe.Services.LLM.Memory.Extraction;
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
    public IMessageChannel Channel { get; }

    private readonly CancellationTokenSource _stopper = new();
    private readonly Channel<BaseProcessEvent> _messages = System.Threading.Channels.Channel.CreateUnbounded<BaseProcessEvent>();
    private readonly string _selfUsername;

    private readonly ulong _memoryContext;
    private readonly IConversationStateStorage _chatStorage;
    private readonly IMemoryExtractAndStoreQueue _memory;

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
    public ProcessingState State { get; private set; } = ProcessingState.Loading;

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
    /// <param name="memoryContext"></param>
    /// <param name="conversation"></param>
    /// <param name="channel"></param>
    /// <param name="client"></param>
    /// <param name="chatStorage"></param>
    /// <param name="memory"></param>
    public LlmChatConversation(
        ulong memoryContext,
        ChatConversation conversation,
        IMessageChannel channel,
        DiscordSocketClient client,
        IConversationStateStorage chatStorage,
        IMemoryExtractAndStoreQueue memory
    )
    {
        Channel = channel;    

        _selfUsername = $"@{client.CurrentUser.Username}";
        _memoryContext = memoryContext;
        _chatStorage = chatStorage;
        _memory = memory;

        Task.Run(async () => await MessageConsumer(conversation));
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
            State = ProcessingState.Loading;

            // Try to load conversation state
            var state = await _chatStorage.Get(Channel.Id);
            if (state != null)
            {
                conversation.Load(state.Json);
                Summary = conversation.FindSummaryMessage();
            }
            UpdateStats();

            State = ProcessingState.Waiting;

            // Event processing loop, processes:
            // - A message arrives that needs a response
            // - A summary is needed and a timeout happens
            var reader = _messages.Reader;
            var summaryNeeded = false;
            while (!_stopper.IsCancellationRequested && !reader.Completion.IsCompleted)
            {
                State = ProcessingState.Waiting;

                // Wait for an event:
                // - Something is ready to read
                // - Timeout to auto summary
                // - Cancellation (_stopper throws an exception)
                var waitResult = await reader.WaitToReadWithTimeout(summaryNeeded ? TimeSpan.FromMinutes(2) : TimeSpan.FromDays(1), _stopper.Token);

                // Check if no more messages will ever arrive: break out of loop.
                if (waitResult == ChannelReaderExtensions.WaitToReadResult.EndOfStream)
                {
                    Log.Warning("EndOfStream for conversation processor for channel {0} ({1})", Channel.Name, Channel.Id);
                    break;
                }

                // Check if the timeout occured: auto summarise
                if (waitResult == ChannelReaderExtensions.WaitToReadResult.Timeout)
                {
                    Log.Information("LLM conversation auto summarisation for '{0}'", Channel.Name);

                    if (summaryNeeded)
                    {
                        State = ProcessingState.Summarising;
                        await Summarise();
                        summaryNeeded = false;
                        State = ProcessingState.Waiting;
                    }

                    continue;
                }

                if (waitResult == ChannelReaderExtensions.WaitToReadResult.ReadyToRead)
                {
                    // We know the channel is ready to be read from, read now.
                    var @event = await reader.ReadAsync(_stopper.Token);

                    switch (@event)
                    {
                        case BaseProcessEvent.Message message:
                        {
                            // LLM call to generate a response
                            using (Channel.EnterTypingState())
                            {
                                State = ProcessingState.Generating;
                                var response = await GenerateResponse(message, cancellation: _stopper.Token);
                                summaryNeeded = true;
                                if (!string.IsNullOrWhiteSpace(response))
                                    await Channel.SendLongMessageAsync(response);
                                else
                                    Log.Warning("LLM conversation failed to generate response");
                                State = ProcessingState.Waiting;
                            }

                            // Run cleanup
                            if (await Cleanup())
                                summaryNeeded = false;

                            break;
                        }

                        case BaseProcessEvent.Clear:
                        {
                            conversation.Clear();
                            summaryNeeded = false;
                            Summary = null;
                            break;
                        }

                        case BaseProcessEvent.Summarise:
                        {
                            await Summarise();
                            summaryNeeded = false;
                            break;
                        }
                    }

                    @event.Complete();
                }

                // Always update stats
                UpdateStats();

                // Always save state
                await _chatStorage.Put(Channel.Id, new(conversation.Save()));
            }
        }
        catch (OperationCanceledException)
        {
            Log.Warning("Conversation processor OperationCanceledException for channel {0} ({1})", Channel.Name, Channel.Id);
            await _chatStorage.Put(Channel.Id, new(conversation.Save()));
            throw;
        }
        catch (Exception ex)
        {
            Log.Error("Exception killed conversation processor for channel {0} ({1}): {2}", Channel.Name, Channel.Id, ex);
        }
        finally
        {
            Log.Warning("Conversation processor marked IsComplete=true for channel {0} ({1})", Channel.Name, Channel.Id);
            IsComplete = true;
        }

        void UpdateStats()
        {
            MessageCount = conversation.MessageCount;
            ContextUsage = conversation.EstimateTokenCount() / (float)contextTokens;
        }

        async ValueTask<bool> Cleanup()
        {
            var summarised = false;

            // Clean up "buried" tool messages
            conversation.CleanBuriedToolMessages(3);

            // Summarise the conversation if there's no work pending
            if (conversation.TotalTokens > LowCompressThreshold && _messages.Reader.Count == 0)
            {
                await Summarise();
                summarised = true;
            }

            // Summarise the conversation even if there's work pending
            else if (conversation.TotalTokens > MidCompressThreshold)
            {
                summarised = true;
                await Summarise();
            }

            // Summarisation failed, try to sweep up tool messages. Removing them oldest first until
            // We're below the target size.
            if (conversation.TotalTokens > HighCompressThreshold)
            {
                conversation.SweepToolMessages(HighCompressThreshold);
            }

            // Summarisation failed, just clear the state.
            if (conversation.TotalTokens > HighCompressThreshold)
            {
                Log.Warning("Compression failed for conversation state in channel {0} ({1})", Channel.Name, Channel.Id);
                conversation.Clear();
                Summary = null;

                // Technically we didn't summarise, but we did something even more aggressive so return true
                summarised = true;
            }

            return summarised;
        }

        async ValueTask Summarise()
        {
            var transcript = conversation.Transcript("Assistant");
            await _memory.Enqueue(_memoryContext, transcript);

            Summary = await conversation.AutoSummarise(_stopper.Token);
        }

        async ValueTask<string?> GenerateResponse(BaseProcessEvent.Message context, int maxIters = 8, CancellationToken cancellation = default)
        {
            conversation.AddUserMessage(
                context.User.GlobalName ?? context.User.Username,
                context.Content
            );

            return await conversation.GenerateResponseMultiStep(maxIters, cancellation);
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
            .TrimStart(_selfUsername)
            .TrimStart();

        // Enqueue work
        await _messages.Writer.WriteAsync(new BaseProcessEvent.Message(context.User, new string(content)), ct);
    }

    /// <summary>
    /// Clear this conversation state
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task Clear()
    {
        var evt = new BaseProcessEvent.Clear();
        await _messages.Writer.WriteAsync(evt);
        await evt.Completed;
    }

    /// <summary>
    /// Force summarisation of this state
    /// </summary>
    /// <returns></returns>
    public async Task ForceSummary()
    {
        var evt = new BaseProcessEvent.Summarise();
        await _messages.Writer.WriteAsync(evt);
        await evt.Completed;
    }

    private abstract record BaseProcessEvent
    {
        private readonly TaskCompletionSource _completionSource = new();

        public Task Completed => _completionSource.Task;

        public void Complete()
        {
            _completionSource.SetResult();
        }

        public sealed record Message(IUser User, string Content) : BaseProcessEvent;

        public sealed record Clear : BaseProcessEvent;

        public sealed record Summarise : BaseProcessEvent;
    }

    /// <summary>
    /// Current state of the processing task for this conversation
    /// </summary>
    public enum ProcessingState
    {
        /// <summary>
        /// Waiting for a message to process
        /// </summary>
        Waiting,

        /// <summary>
        /// Generating an response using the LLM
        /// </summary>
        Generating,

        /// <summary>
        /// Running context summarisation
        /// </summary>
        Summarising,

        /// <summary>
        /// Interacting with persistent storage
        /// </summary>
        Loading,

        /// <summary>
        /// Interacting with persistent storage
        /// </summary>
        Saving,
    }
}