using Discord;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Mute.Moe.Discord.Services.Users;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Mute.Moe.Services.LLM.Chat.Middleware;

/// <summary>
/// Add some metadata to the start of user messages
/// </summary>
public class PrependMessageMetadata
    : IAgentMiddleware
{
    private readonly IDiscordClient _client;
    private readonly IUserService _users;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrependMessageMetadata"/> class.
    /// </summary>
    /// <param name="client">The Discord client used to fetch guild and user information.</param>
    /// <param name="users">The user service used to retrieve user-related data.</param>
    public PrependMessageMetadata(IDiscordClient client, IUserService users)
    {
        _client = client;
        _users = users;
    }
    
    /// <inheritdoc />
    public async Task<AgentResponse> Middleware(IEnumerable<ChatMessage> messages, AgentSession? session, AgentRunOptions? options, AIAgent innerAgent, CancellationToken cancellationToken)
    {
        return await innerAgent.RunAsync(
            await ModifyMessages(messages, cancellationToken).ToListAsync(cancellationToken),
            session,
            options,
            cancellationToken
        ).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<AgentResponseUpdate> MiddlewareStreaming(IEnumerable<ChatMessage> messages, AgentSession? session, AgentRunOptions? options, AIAgent innerAgent, CancellationToken cancellationToken)
    {
        var results = innerAgent.RunStreamingAsync(
            await ModifyMessages(messages, cancellationToken).ToListAsync(cancellationToken),
            session,
            options,
            cancellationToken
        );

        await foreach (var result in results)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return result;
        }
    }

    private async IAsyncEnumerable<ChatMessage> ModifyMessages(IEnumerable<ChatMessage> messages, [EnumeratorCancellation] CancellationToken cancellation)
    {
        foreach (var message in messages)
            yield return await ReplaceMessage(message, cancellation);
    }

    private async Task<ChatMessage> ReplaceMessage(ChatMessage message, CancellationToken cancellation)
    {
        if (message.Role != ChatRole.User)
            return message;

        // Get the metadata
        var name = await TryGetAuthorName(message.AuthorName, message.AdditionalProperties, cancellation);
        var time = await TryGetMessageTime(message.AdditionalProperties);

        // Clone the message before modifying it
        message = message.Clone();
        message.Contents = message.Contents.ToList();

        // Modify each TextContent item
        for (var i = 0; i < message.Contents.Count; i++)
        {
            if (message.Contents[i] is TextContent tc)
            {
                message.Contents[i] = new TextContent(ApplyFormatting(tc.Text, name, time))
                {
                    AdditionalProperties = tc.AdditionalProperties,
                    Annotations = tc.Annotations
                };
            }
        }

        return message;
    }

    private async Task<string?> TryGetAuthorName(string? authorName, AdditionalPropertiesDictionary? props, CancellationToken cancellation)
    {
        if (props != null)
        {
            if (props.TryGetValue<ulong>(MessageMetadataKeys.U64_DiscordAuthorId, out var discordUserId))
            {
                // Try to get the channel this was sent in
                IChannel? channel = null;
                if (props.TryGetValue<ulong>(MessageMetadataKeys.U64_DiscordChannelId, out var discordChannelId))
                {
                    cancellation.ThrowIfCancellationRequested();
                    channel = await _client.GetChannelAsync(discordChannelId);
                }

                // Get the best name we can manage
                cancellation.ThrowIfCancellationRequested();
                return await _users.Name(discordUserId, (channel as IGuildChannel)?.Guild, mention:false);
            }
        }

        return authorName;
    }

    private async Task<DateTime?> TryGetMessageTime(AdditionalPropertiesDictionary? props)
    {
        if (props == null)
            return null;

        if (!props.TryGetValue(MessageMetadataKeys.U64_Timestamp, out ulong unix))
            return null;

        return unix.FromUnixTimestamp();
    }
    
    private static string ApplyFormatting(string text, string? name, DateTime? time)
    {
        var timeStr = time?.ToShortTimeString() + "Z";
        return (name == null, time == null) switch
        {
            (false, false) => $"[{name}@{timeStr}]: {text}",
            (false, true) => $"[{name}]: {text}",
            (true, false) => $"[UNKNOWN@{timeStr}]: {text}",
            (true, true) => text,
        };
    }
}