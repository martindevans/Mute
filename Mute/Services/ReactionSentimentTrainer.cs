using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using JetBrains.Annotations;
using Mute.Responses;

namespace Mute.Services
{
    public class ReactionSentimentTrainer
    {
        private readonly DiscordSocketClient _client;
        private readonly SentimentService _sentiment;

        public ReactionSentimentTrainer(DiscordSocketClient client, SentimentService sentiment)
        {
            _client = client;
            _sentiment = sentiment;

            _client.ReactionAdded += OnReactionAdded;
        }

        private async Task OnReactionAdded(Cacheable<IUserMessage, ulong> message, [NotNull] ISocketMessageChannel channel, [NotNull] SocketReaction reaction)
        {
            if (SentimentResponse.Happy.Contains(reaction.Emote))
                await TryLearn(await message.DownloadAsync(), reaction, true);
            else if (SentimentResponse.Sad.Contains(reaction.Emote))
                await TryLearn(await message.DownloadAsync(), reaction, false);
        }

        private async Task TryLearn([NotNull] IUserMessage message, [NotNull] IReaction reaction, bool sentiment)
        {
            var users = await message.GetReactionUsersAsync(reaction.Emote);
            if (users.Count >= 3 || users.Any(IsTeacher))
                await _sentiment.Teach(message.Content, sentiment);
        }

        private bool IsTeacher([NotNull] IUser user)
        {
            //For now hardcode it to Nyarlathothep only, in the future make this role based
            return user.Id == 103509816437149696;
        }
    }
}
