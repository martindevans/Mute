using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Mute.Moe.Discord.Context;
using Serilog;
using IEnumerableExtensions = Mute.Moe.Extensions.IEnumerableExtensions;

namespace Mute.Moe.Discord.Services.Responses;

/// <summary>
/// Automatically responds to messages that aren't commands.
/// </summary>
public class ConversationalResponseService
{
    private readonly DiscordSocketClient _client;
    private readonly Random _random;
    private readonly List<IResponse> _responses = [ ];

    private readonly ConcurrentDictionary<IUser, IConversation?> _conversations = new();

    /// <summary>
    /// Create a new <see cref="ConversationalResponseService"/>
    /// </summary>
    /// <param name="client"></param>
    /// <param name="services"></param>
    /// <param name="random"></param>
    public ConversationalResponseService(DiscordSocketClient client, IServiceProvider services, Random random)
    {
        _client = client;
        _random = random;

        // Get every type that implements IResponse
        _responses.AddRange(from t in Assembly.GetExecutingAssembly().GetTypes()
            where t.IsClass
            where typeof(IResponse).IsAssignableFrom(t)
            let i = ActivatorUtilities.CreateInstance(services, t) as IResponse
            where i != null
            select i
        );

        foreach (var response in _responses)
            Log.Information("Loaded response generator: {0}", response.GetType().Name);
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

        // Get a conversation with this user (either continued from before, or starting with a new one)
        var conversation = await GetOrCreateConversation(context, mentionsBot);

        // If we have a conversation, use it to respond
        if (conversation != null)
        {
            var response = await conversation.Respond(context, mentionsBot, CancellationToken.None);
            if (response != null)
                await context.Channel.TypingReplyAsync(response);
        }
    }

    private async Task<IConversation?> GetOrCreateConversation(MuteCommandContext context, bool mentionsBot)
    {
        // Try to get the existing conversation
        if (_conversations.TryGetValue(context.User, out var conversation) && conversation != null && !conversation.IsComplete)
            return conversation;

        // Insert new conversation
        var newConv = await TryCreateConversation(context, mentionsBot);
        _conversations[context.User] = newConv;
        return newConv;
    }

    private async Task<IConversation?> TryCreateConversation(MuteCommandContext context, bool mentionsBot)
    {
        // Find generators which can respond to this message
        var random = new Random(context.Message.Id.GetHashCode());
        var candidates = new List<IConversation>();
        foreach (var generator in _responses)
        {
            try
            {
                var conversation = await generator.TryRespond(context, mentionsBot);
                if (conversation == null)
                    continue;

                var rand = random.NextDouble();
                if ((mentionsBot && rand < generator.MentionedChance) || (!mentionsBot && rand < generator.BaseChance))
                    candidates.Add(conversation);
            }
            catch (Exception e)
            {
                Log.Error(e, "Response generator {0} failed with exception", generator.GetType().Name);
            }
        }

        if (candidates.Count == 0)
            return null;

        // If there are several pick a random one
        return IEnumerableExtensions.Random(candidates, _random);
    }

    /// <summary>
    /// Get the conversation the current user is in
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    public IConversation? GetConversation(IGuildUser user)
    {
        return _conversations.GetValueOrDefault(user);
    }
}