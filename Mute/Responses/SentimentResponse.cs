using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Mute.Extensions;
using Mute.Services;

namespace Mute.Responses
{
    public class SentimentResponse
        : IResponse
    {
        private readonly SentimentService _sentiment;
        private readonly Random _random;

        private const double Bracket = 0.2;

        public bool RequiresMention => false;
        public double Chance => 0.1;

        private readonly Emoji[] _sad = {
            EmojiLookup.BrokenHeart,
            EmojiLookup.ThumbsDown,
            EmojiLookup.Worried,
            EmojiLookup.Pensive,
            EmojiLookup.SlightlyFrowning,
            EmojiLookup.Crying
        };

        private readonly Emoji[] _happy = {
            EmojiLookup.Heart,
            EmojiLookup.ThumbsUp,
            EmojiLookup.Grin,
            EmojiLookup.Smile,
        };

        public SentimentResponse(SentimentService sentiment, Random random)
        {
            _sentiment = sentiment;
            _random = random;
        }

        public async Task<bool> MayRespond(IMessage message, bool containsMention)
        {
            var s = await _sentiment.Sentiment(message.Content);
            return s < Bracket || s > (1 - Bracket);
        }

        public async Task<string> Respond(IMessage message, bool containsMention, CancellationToken ct)
        {
            if (message is IUserMessage umsg)
            {
                var s = await _sentiment.Sentiment(message.Content);

                if (s < Bracket)
                    await umsg.AddReactionAsync(_sad.Random(_random));
                else if (s > (1 - Bracket))
                    await umsg.AddReactionAsync(_happy.Random(_random));
            }

            return null;
        }
    }
}
