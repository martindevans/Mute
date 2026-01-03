using Discord;
using Discord.WebSocket;
using Mute.Moe.Discord.Context;
using Mute.Moe.Services.LLM;
using Mute.Moe.Utilities;
using System.Threading.Tasks;
using Serilog;

namespace Mute.Moe.Discord.Services.Responses;

/// <summary>
/// Maintains an LLM conversation thread per channel.
/// </summary>
public class ConversationalResponseService
{
    private readonly DiscordSocketClient _client;
    private readonly ChatConversationFactory _chatFactory;

    private readonly AsyncLock _lookupLock = new();
    private readonly Dictionary<ulong, LlmChatConversation> _conversationsByChannel = [ ];

    /// <summary>
    /// Create a new <see cref="ConversationalResponseService"/>
    /// </summary>
    /// <param name="client"></param>
    /// <param name="chatFactory"></param>
    public ConversationalResponseService(DiscordSocketClient client, ChatConversationFactory chatFactory)
    {
        _client = client;
        _chatFactory = chatFactory;
    }

    /// <summary>
    /// Try to respond to the given context
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task Respond(MuteCommandContext context)
    {
        // Check if the bot is directly mentioned
        var mentionsBot = ((IMessage)context.Message).MentionedUserIds.Contains(_client.CurrentUser.Id);

        // Ignore messages that don't mention the bot directly
        if (!mentionsBot)
            return;

        // Get the conversation for this channel
        var conversation = await GetOrCreateConversation(context, mentionsBot);

        // If we have a conversation, use it to respond
        if (conversation != null)
            await conversation.Enqueue(context);
    }

    private async Task<LlmChatConversation?> GetOrCreateConversation(MuteCommandContext context, bool mentionsBot)
    {
        // Ignore messages not addressed to bot
        if (!mentionsBot)
            return null;

        // Only allow one user of the map at once
        using (await _lookupLock.LockAsync())
        {
            // Remove dead conversations
            foreach (var (channelId, conv) in _conversationsByChannel)
            {
                if (conv.IsComplete)
                {
                    await conv.Stop();
                    _conversationsByChannel.Remove(channelId);
                }
            }

            // Unload conversations which haven't been used for a while
            foreach (var (channelId, conv) in _conversationsByChannel)
            {
                var elapsed = DateTime.UtcNow - conv.LastUpdated;
                if (elapsed > TimeSpan.FromMinutes(15))
                {
                    await conv.Stop();
                    Save(channelId, conv);
                    _conversationsByChannel.Remove(channelId);
                }
            }

            // Get or create conversation for channel
            if (!_conversationsByChannel.TryGetValue(context.Channel.Id, out var chat))
            {
                chat = TryLoad(context.Channel.Id)
                    ?? new LlmChatConversation(await _chatFactory.Create(context.Channel), context.Channel, _client);

                _conversationsByChannel[context.Channel.Id] = chat;
            }
            return chat;
        }
    }

    private void Save(ulong channelId, LlmChatConversation conv)
    {
        Log.Information("todo: implement channel conversation saving persistence");
    }

    private LlmChatConversation? TryLoad(ulong channelId)
    {
        Log.Information("todo: implement channel conversation loading persistence");
        return null;
    }

    /// <summary>
    /// Get the conversation in the given channel
    /// </summary>
    /// <param name="channel"></param>
    /// <returns></returns>
    public async Task<LlmChatConversation?> GetConversation(ISocketMessageChannel channel)
    {
        using (await _lookupLock.LockAsync())
            return _conversationsByChannel.GetValueOrDefault(channel.Id);
    }
}