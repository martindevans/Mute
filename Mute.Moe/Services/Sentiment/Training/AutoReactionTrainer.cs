using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

using Mute.Moe.Discord.Services.Responses;
using Mute.Moe.Services.Host;

namespace Mute.Moe.Services.Sentiment.Training;

public class AutoReactionTrainer
    : IHostedService
{
    private readonly ISentimentTrainer _sentiment;
    private readonly BaseSocketClient _client;

    public AutoReactionTrainer(BaseSocketClient client, ISentimentTrainer sentiment)
    {
        _sentiment = sentiment;
        _client = client;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _client.ReactionAdded += OnReactionAdded;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _client.ReactionAdded -= OnReactionAdded;
    }

    private async Task OnReactionAdded(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
    {
        if (SentimentResponse.Happy.Contains(reaction.Emote.Name))
            await TryLearn(await message.DownloadAsync(), reaction, Sentiment.Positive);
        else if (SentimentResponse.Sad.Contains(reaction.Emote.Name))
            await TryLearn(await message.DownloadAsync(), reaction, Sentiment.Negative);
        else if (SentimentResponse.Neutral.Contains(reaction.Emote.Name))
            await TryLearn(await message.DownloadAsync(), reaction, Sentiment.Neutral);
    }

    private async Task TryLearn(IMessage message, IReaction reaction, Sentiment sentiment)
    {
        //Early exit if channel is not a guild channel
        if (message.Channel is not SocketGuildChannel gc)
            return;
        var g = gc.Guild;

        //Get guild users who reacted to the message
        var users = await message.GetReactionUsersAsync(reaction.Emote, 128).Flatten().Select(u => u as IGuildUser ?? g.GetUser(u.Id)).Where(u => u != null).Distinct().ToArrayAsync();

        if (users.Length >= 3 || users.Any(IsTeacher))
            await _sentiment.Teach(message.Content, sentiment);
    }

    private static bool IsTeacher(IGuildUser user)
    {
        //Check if the user has the `*MuteTeacher` role (hardcoded ID for now)
        return user.RoleIds.Contains<ulong>(506127510740795393);
    }
}