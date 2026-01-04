using Discord;
using Discord.WebSocket;
using LlmTornado.Chat;
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
    public IMessageChannel Channel { get; }

    private readonly CancellationTokenSource _stopper = new();
    private readonly Channel<BaseProcessEvent> _messages = System.Threading.Channels.Channel.CreateUnbounded<BaseProcessEvent>();
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
    /// <param name="conversation"></param>
    /// <param name="channel"></param>
    /// <param name="client"></param>
    /// <param name="chatStorage"></param>
    public LlmChatConversation(ChatConversation conversation, IMessageChannel channel, DiscordSocketClient client, IConversationStateStorage chatStorage)
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

                // Wait for a message to arrive, or some time to pass with no messages
                var readTask = reader.ReadAsync(_stopper.Token).AsTask();
                var delayTask = Task.Delay(summaryNeeded ? TimeSpan.FromMinutes(2) : TimeSpan.FromDays(1), _stopper.Token);
                var completed = await Task.WhenAny(readTask, delayTask);

                // Break out of loop if cancelled
                if (_stopper.IsCancellationRequested)
                    break;

                // Check if we're here due to the timeout or a message event
                if (completed == delayTask)
                {
                    Log.Information("LLM conversation auto summarisation for '{0}'", Channel.Name);

                    if (summaryNeeded)
                    {
                        State = ProcessingState.Summarising;
                        await Summarise();
                        summaryNeeded = false;
                        State = ProcessingState.Waiting;
                    }
                }
                else
                {
                    // Using `Result` is ok here, the task was awaited above
                    var @event = readTask.Result;

                    switch (@event)
                    {
                        case BaseProcessEvent.Message message:
                        {
                            // LLM call to generate a response
                            using (Channel.EnterTypingState())
                            {
                                State = ProcessingState.Generating;
                                var response = await GenerateResponse(message);
                                summaryNeeded = true;
                                if (!string.IsNullOrWhiteSpace(response))
                                    await Channel.SendLongMessageAsync(response);
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
                await _chatStorage.Put(Channel.Id, new ConversationStateData(conversation.Save()));
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

        void UpdateStats()
        {
            MessageCount = conversation.MessageCount;
            ContextUsage = conversation.EstimateTokenCount() / (float)contextTokens;
        }

        async ValueTask<bool> Cleanup()
        {
            var summarised = false;

            // Clean up "buried" tool messages
            conversation.CleanToolMessages(3);

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

            // Summarisation failed, just clear the state.
            if (conversation.TotalTokens > HighCompressThreshold)
            {
                Log.Warning("Compression failed for conversation state in channel {0} ({1})", Channel.Name, Channel.Id);
                conversation.Clear();
                Summary = null;
                summarised = true;
            }

            return summarised;
        }

        async ValueTask Summarise()
        {
            Summary = await conversation.AutoSummarise(_stopper.Token);
        }

        async ValueTask<string?> GenerateResponse(BaseProcessEvent.Message context)
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

            return response;
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