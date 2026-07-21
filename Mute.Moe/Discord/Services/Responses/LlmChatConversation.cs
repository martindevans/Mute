using Discord;
using HandyAgentFramework.Persistence;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Mute.Moe.Discord.Context;
using Mute.Moe.Tools;
using System.Net.Http;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Mute.Moe.Services.LLM.Chat;

namespace Mute.Moe.Discord.Services.Responses;

/// <summary>
/// Factory class responsible for creating instances of <see cref="LlmChatConversation"/>.
/// </summary>
public class LlmChatConversationFactory
{
    private readonly IDiscordClient _discord;
    private readonly ChatAgentFactory _agentFactory;
    private readonly ISessionStore _chatStorage;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<LlmChatConversation> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LlmChatConversationFactory"/> class.
    /// </summary>
    /// <param name="discord">
    /// The Discord client used for interacting with Discord services.
    /// </param>
    /// <param name="agentFactory">
    /// The factory responsible for creating chat agents.
    /// </param>
    /// <param name="chatStorage">
    /// The session store used for persisting chat data.
    /// </param>
    /// <param name="httpFactory">
    /// The HTTP client factory used for creating HTTP clients.
    /// </param>
    /// <param name="logger">
    /// The logger instance for logging messages related to <see cref="LlmChatConversation"/>.
    /// </param>
    public LlmChatConversationFactory(
        IDiscordClient discord,
        ChatAgentFactory agentFactory,
        ISessionStore chatStorage,
        IHttpClientFactory httpFactory,
        ILogger<LlmChatConversation> logger
    )
    {
        _discord = discord;
        _agentFactory = agentFactory;
        _chatStorage = chatStorage;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    /// <summary>
    /// Create an LLM chat conversation bound to the given channel
    /// </summary>
    /// <param name="channel"></param>
    /// <returns></returns>
    public async Task<LlmChatConversation> Create(IMessageChannel channel)
    {
        var template = new Dictionary<string, string>
        {
            { "self_name", _discord.CurrentUser.Username },
            { "guild", channel is IDMChannel ? "none (direct message)" : ((IGuildChannel)channel).Guild.Name },
            { "channel", channel.Name },
        };

        return new LlmChatConversation(
            await _agentFactory.Create(template),
            _agentFactory.ContextSize,
            channel,
            _discord,
            _chatStorage,
            _httpFactory,
            _logger
        );
    }
}

/// <summary>
/// A chat conversation that uses LLMs to respond
/// </summary>
public partial class LlmChatConversation
{
    /// <summary>
    /// The channel this conversation is bound to
    /// </summary>
    public IMessageChannel Channel { get; }

    private readonly CancellationTokenSource _stopper = new();
    private readonly Channel<BaseProcessEvent> _messages = System.Threading.Channels.Channel.CreateUnbounded<BaseProcessEvent>();
    private readonly string _selfUsername;

    private readonly AIAgent _agent;
    private readonly int _contextSize;
    private readonly ISessionStore _sessionStorage;
    private readonly IHttpClientFactory _httpClient;
    private readonly AgentRunOptions? _options;

    private readonly ILogger<LlmChatConversation> _logger;

    private readonly string _sessionContext;
    private readonly string _sessionKey;

    /// <summary>
    /// Indicates if this conversation is complete and should not be used again
    /// </summary>
    public bool IsComplete { get; private set; }

    /// <summary>
    /// Number of messages waiting for processing
    /// </summary>
    public int QueueCount => _messages.Reader.Count;

    /// <summary>
    /// The current state of the processing queue for this conversation
    /// </summary>
    public ProcessingState State { get; private set; } = ProcessingState.Loading;

    /// <summary>
    /// The last time this conversation state was updated
    /// </summary>
    public DateTime LastUpdated { get; private set; }

    /// <summary>
    /// Context usage statistics from the last generation call
    /// </summary>
    public ContextStats ContextStatistics { get; private set; }

    /// <summary>
    /// Create a new <see cref="LlmChatConversation"/> for the given channel.
    /// </summary>
    /// <param name="agent"></param>
    /// <param name="contextSize"></param>
    /// <param name="channel"></param>
    /// <param name="client"></param>
    /// <param name="sessionStorage"></param>
    /// <param name="httpClient"></param>
    /// <param name="logger"></param>
    public LlmChatConversation(
        AIAgent agent,
        int contextSize,
        IMessageChannel channel,
        IDiscordClient client,
        ISessionStore sessionStorage,
        IHttpClientFactory httpClient,
        ILogger<LlmChatConversation> logger
    )
    {
        Channel = channel;    

        _selfUsername = $"@{client.CurrentUser.Username}";
        _agent = agent;
        _contextSize = contextSize;
        _httpClient = httpClient;
        _logger = logger;

        _sessionStorage = sessionStorage;
        _sessionContext = channel.GetAgentMemoryContextId().ToString();
        _sessionKey = Channel.Id.ToString();

        _options = new AgentRunOptions();
        _options.AttachMuteContext(new MuteAgentContext(channel));

        ContextStatistics = new(contextSize);


        Task.Run(async () => await MessageConsumer());
    }

    private async Task MessageConsumer()
    {
        try
        {
            State = ProcessingState.Waiting;

            await foreach (var @event in _messages.Reader.ReadAllAsync(_stopper.Token))
            {
                // Load conversation state
                State = ProcessingState.Loading;
                await using (var session = await _sessionStorage.GetSessionScope(_sessionContext, _sessionKey, _agent, _stopper.Token))
                {
                    // Process the event
                    State = ProcessingState.Generating;
                    switch (@event)
                    {
                        case BaseProcessEvent.Message message:
                        {
                            using (Channel.EnterTypingState())
                            {
                                var response = await GenerateResponse(session.Session, message, cancellation: _stopper.Token);
                                if (!string.IsNullOrWhiteSpace(response))
                                    await Channel.SendLongMessageAsync(response);
                                else
                                    _logger.LogWarning("LLM conversation failed to generate response");
                            }
                            break;
                        }

                        case BaseProcessEvent.Clear:
                        {
                            session.Session.SetInMemoryChatHistory([]);
                            ContextStatistics = new ContextStats(_contextSize);
                            break;
                        }
                    }

                    // Mark the event as completed
                    @event.Complete();

                    // We're about to save state (done automatically at closing brace). Set the saving state.
                    State = ProcessingState.Saving;
                }

                // Enter waiting state before looping around
                State = ProcessingState.Waiting;
            }
        }
        catch (OperationCanceledException)
        {
            LogConversationOperationCanceledException(Channel.Name, Channel.Id);
            throw;
        }
        catch (Exception ex)
        {
            LogConversationException(Channel.Name, Channel.Id, ex);
            throw;
        }
        finally
        {
            LogConversationIsCompleted(Channel.Name, Channel.Id);
            IsComplete = true;
        }

        async ValueTask<string?> GenerateResponse(AgentSession session, BaseProcessEvent.Message input, CancellationToken cancellation = default)
        {
            // Create message with text content
            var message = new ChatMessage
            {
                AuthorName = input.User.GlobalName ?? input.User.Username,
                Contents = [
                    new TextContent(input.Content)
                ],
                AdditionalProperties = new()
                {
                    { MessageMetadataKeys.U64_DiscordAuthorId, input.User.Id },
                    { MessageMetadataKeys.U64_Timestamp, DateTime.UtcNow.UnixTimestamp() }
                }
            };

            // Attach images
            if (input.AttachedImageUrls.Length > 0)
            {
                using var http = _httpClient.CreateClient();

                // Start downloading all images
                var downloads = input
                    .AttachedImageUrls
                    // ReSharper disable once AccessToDisposedClosure
                    .Select(url => http.GetAsync(url, cancellation))
                    .ToList();

                // Process them in order
                foreach (var task in downloads)
                {
                    // Skip failed requests
                    using var httpResponse = await task;
                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        message.Contents.Add(new TextContent($"Image failed to load! Response code: {httpResponse.StatusCode}"));
                        continue;
                    }

                    // Add content to message
                    var mime = httpResponse.Content.Headers.ContentType?.MediaType;
                    await using var content = await httpResponse.Content.ReadAsStreamAsync(cancellation);
                    message.Contents.Add(await DataContent.LoadFromAsync(content, mime, cancellation));
                }
            }
            
            // Generate LLM response
            var response = await _agent.RunAsync(message, session, _options, cancellation);

            // Extract stats
            if (response.Usage is UsageDetails usage)
            {
                session.TryGetInMemoryChatHistory(out var messages);
                ContextStatistics = new ContextStats(
                    _contextSize,
                    usage,
                    messages?.Count
                );
            }

            return response.Text;
        }
    }

    #region event input
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

        // Get all images in the message
        var images = context.Message.GetMessageImageUrls().ToArray();

        // Strip bot mention from start of string
        var content = context.Message.Resolve(userHandling: TagHandling.Name).AsSpan();
        content = content.Trim();
        content = content.TrimStart(_selfUsername);
        content = content.TrimStart();

        // Enqueue work
        await _messages.Writer.WriteAsync(new BaseProcessEvent.Message(
            context.User,
            new string(content),
            images
        ), ct);
    }

    /// <summary>
    /// Clear this conversation state
    /// </summary>
    /// <returns></returns>
    public async Task Clear()
    {
        var evt = new BaseProcessEvent.Clear();
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

        public sealed record Message(IUser User, string Content, string[] AttachedImageUrls) : BaseProcessEvent;

        public sealed record Clear : BaseProcessEvent;
    }
    #endregion

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
        /// Generating a response using the LLM
        /// </summary>
        Generating,

        /// <summary>
        /// Interacting with persistent storage
        /// </summary>
        Loading,

        /// <summary>
        /// Interacting with persistent storage
        /// </summary>
        Saving,
    }

    /// <summary>
    /// Statistics about context usage
    /// </summary>
    /// <param name="ContextSize">Total context size</param>
    /// <param name="Usage">Usage stats</param>
    /// <param name="Messages">Total message count</param>
    public record ContextStats(
        int ContextSize,
        UsageDetails? Usage = default,
        int? Messages = default
    );

    #region logging
    [LoggerMessage(LogLevel.Warning, "Conversation processor OperationCanceledException for channel {name} ({id})")]
    private partial void LogConversationOperationCanceledException(string name, ulong id);
    
    [LoggerMessage(LogLevel.Error, "Exception killed conversation processor for channel {name} ({id})")]
    private partial void LogConversationException(string name, ulong id, Exception ex);

    [LoggerMessage(LogLevel.Warning, "Conversation processor marked IsComplete=true for channel {name} ({id})")]
    private partial void LogConversationIsCompleted(string name, ulong id);
    #endregion
}