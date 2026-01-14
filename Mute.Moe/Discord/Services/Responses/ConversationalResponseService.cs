using Discord;
using Discord.WebSocket;
using Mute.Moe.Discord.Context;
using Mute.Moe.Services.LLM;
using Mute.Moe.Utilities;
using System.Threading.Tasks;

namespace Mute.Moe.Discord.Services.Responses;

/// <summary>
/// Maintains an LLM conversation thread per channel.
/// </summary>
public class ConversationalResponseService
{
    private readonly DiscordSocketClient _client;
    private readonly ChatConversationFactory _chatFactory;
    private readonly IConversationStateStorage _chatStorage;

    private readonly AsyncLock _lookupLock = new();
    private readonly Dictionary<ulong, LlmChatConversation> _conversationsByChannel = [ ];

    /// <summary>
    /// Create a new <see cref="ConversationalResponseService"/>
    /// </summary>
    /// <param name="client"></param>
    /// <param name="chatFactory"></param>
    /// <param name="chatStorage"></param>
    public ConversationalResponseService(DiscordSocketClient client, ChatConversationFactory chatFactory, IConversationStateStorage chatStorage)
    {
        _client = client;
        _chatFactory = chatFactory;
        _chatStorage = chatStorage;
    }

    /// <summary>
    /// Try to respond to the given context
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task Respond(MuteCommandContext context)
    {
        // Don't ever respond to self!
        if (context.User.Id == _client.CurrentUser.Id)
            return;

        // Is this a DM channel?
        var isDirectMessage = context.Channel.GetChannelType() == ChannelType.DM;

        // Check if the bot is directly mentioned
        var mentionsBot = ((IMessage)context.Message).MentionedUserIds.Contains(_client.CurrentUser.Id);

        // Respond to:
        // - All messages in DMs
        // - Direct mentions in other channels
        if (isDirectMessage || mentionsBot)
        {
            // Get the conversation for this channel
            var conversation = await GetOrCreateConversation(context.Channel);

            // Use it to respond
            await conversation.Enqueue(context);
        }
    }

    private async Task<LlmChatConversation> GetOrCreateConversation(IMessageChannel channel)
    {
        // Only allow one user of the map at once
        using (await _lookupLock.LockAsync())
        {
            // Remove dead conversations
            foreach (var (channelId, conv) in _conversationsByChannel)
                if (conv.IsComplete)
                    _conversationsByChannel.Remove(channelId);

            // Get or create conversation for channel
            if (!_conversationsByChannel.TryGetValue(channel.Id, out var chat))
            {
                chat = new LlmChatConversation(
                    await _chatFactory.Create(channel),
                    channel,
                    _client,
                    _chatStorage
                );

                _conversationsByChannel[channel.Id] = chat;
            }
            return chat;
        }
    }

    /// <summary>
    /// Get the conversation in the given channel
    /// </summary>
    /// <param name="channel"></param>
    /// <returns></returns>
    public async Task<LlmChatConversation> GetConversation(IMessageChannel channel)
    {
        return await GetOrCreateConversation(channel);
    }
}