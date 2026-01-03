using Mute.Moe.Discord.Context;
using Mute.Moe.Services.LLM;
using Serilog;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Mute.Moe.Discord.Services.Responses;

/// <summary>
/// Generate responses to messages using an LLM. Each unique channel has a conversation bound to it, which all users share.
/// </summary>
[UsedImplicitly]
public class LlmChatResponse
    : IResponse
{
    private readonly ChatConversationFactory _chatFactory;

    /// <inheritdoc />
    public double BaseChance => 0;

    /// <inheritdoc />
    public double MentionedChance => 1;

    /// <summary>
    /// All active conversations
    /// </summary>
    private readonly ConcurrentDictionary<ulong, LlmChatConversation> _channelChats = [ ];

    /// <summary>
    /// Create a new <see cref="LlmChatResponse"/>
    /// </summary>
    /// <param name="chatFactory"></param>
    public LlmChatResponse(ChatConversationFactory chatFactory)
    {
        _chatFactory = chatFactory;
    }

    /// <inheritdoc />
    public async Task<IConversation?> TryRespond(MuteCommandContext context, bool containsMention)
    {
        // Ignore messages not addressed to bot
        if (!containsMention)
            return null;

        // Time out conversations
        foreach (var (key, value) in _channelChats)
        {
            if (value.IsComplete || DateTime.UtcNow - value.LastUpdated > TimeSpan.FromMinutes(15))
            {
                if (_channelChats.TryRemove(key, out var removed))
                    await removed.Stop();
                Log.Information("Timed out LLM conversation for channel: {0}", key);
            }
        }

        // Get a conversation for the channel
        if (!_channelChats.TryGetValue(context.Channel.Id, out var chat))
        {
            Log.Information("Creating new LLM conversational state for channel {0}", context.Channel.Name);
            chat = new LlmChatConversation(await _chatFactory.Create(context.Channel), context.Channel);
            _channelChats[context.Channel.Id] = chat;
        }

        return chat;
    }

    /// <summary>
    /// A chat conversation that uses LLMs to respond
    /// </summary>
    public class LlmChatConversation
        : IConversation
    {
        private readonly ISocketMessageChannel _channel;

        private readonly CancellationTokenSource _stopper = new();
        private readonly Channel<MessageContext> _messages = Channel.CreateUnbounded<MessageContext>();

        /// <inheritdoc />
        public bool IsComplete { get; private set; }

        /// <summary>
        /// The last time this conversation was updated
        /// </summary>
        public DateTime LastUpdated { get; private set; }

        /// <summary>
        /// Create a new <see cref="LlmChatConversation"/> for the given channel.
        /// </summary>
        /// <param name="conversation"></param>
        /// <param name="channel"></param>
        public LlmChatConversation(ChatConversation conversation, ISocketMessageChannel channel)
        {
            _channel = channel;

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
            var LowCompressThreshold = (int)MathF.Floor(contextTokens * 0.7f);
            var MidCompressThreshold = (int)MathF.Floor(contextTokens * 0.8f);
            var HighCompressThreshold = (int)MathF.Floor(contextTokens * 0.9f);

            try
            {
                await foreach (var context in _messages.Reader.ReadAllAsync(_stopper.Token))
                {
                    using (_channel.EnterTypingState())
                    {
                        // Add incoming message to conversation
                        await conversation.AddUserMessage(context.User.GlobalName, context.Content);

                        // Generate a response. This takes a long time, since it's making the call to the LLM
                        await conversation.GenerateResponse(_stopper.Token);

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
                            await _channel.SendLongMessageAsync(response);

                        // Clean up "buried" tool messages
                        var removedTools = conversation.CleanToolMessages(3);
                        if (removedTools > 0)
                            Log.Information("Cleaned up {2} tool messages from conversation in channel {0} ({1})", _channel.Name, _channel.Id, removedTools);

                        // Summarise the conversation if there's no work pending
                        if (conversation.TotalTokens > LowCompressThreshold && _messages.Reader.Count == 0)
                            await conversation.Summarise(_stopper.Token);

                        // Summarise the conversation even if there's work pending
                        else if (conversation.TotalTokens > MidCompressThreshold)
                            await conversation.Summarise(_stopper.Token);

                        // Summarisation failed, just clear the state.
                        if (conversation.TotalTokens > HighCompressThreshold)
                        {
                            Log.Warning("Compression failed for conversation state in channel {0} ({1})", _channel.Name, _channel.Id);
                            conversation.Clear();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Exception killed conversation processor for channel {0} ({1}): {2}", _channel.Name, _channel.Id, ex);
            }
            finally
            {
                IsComplete = true;
            }
        }

        /// <inheritdoc />
        public async Task<string?> Respond(MuteCommandContext context, bool containsMention, CancellationToken ct)
        {
            // Ignore messages that do not directly mention the bot
            if (!containsMention)
                return null;

            // Update the last used time
            LastUpdated = DateTime.UtcNow;

            //todo: strip bot mention from start of string

            // Enqueue work
            await _messages.Writer.WriteAsync(new(context.User, context.Message.Resolve()), ct);

            // Always return no reply. The processing loop will post the reply when ready.
            return null;
        }

        private record struct MessageContext(IUser User, string Content);
    }
}