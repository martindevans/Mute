using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using JetBrains.Annotations;
using Mute.Extensions;

namespace Mute.Services.Responses
{
    public class SentimentResponse
        : IResponse
    {
        private readonly SentimentService _sentiment;
        private readonly Random _random;

        private const double Bracket = 0.1;

        public double BaseChance => 0.1;
        public double MentionedChance => 0.25;

        public static readonly IReadOnlyList<Emoji> Sad = new[] {
            EmojiLookup.BrokenHeart,
            EmojiLookup.ThumbsDown,
            EmojiLookup.Worried,
            EmojiLookup.Pensive,
            EmojiLookup.SlightlyFrowning,
            EmojiLookup.Crying
        };

        public static readonly IReadOnlyList<Emoji> Happy = new[] {
            EmojiLookup.Heart,
            EmojiLookup.ThumbsUp,
            EmojiLookup.Grin,
            EmojiLookup.Smile,
            EmojiLookup.SlightSmile
        };

        public SentimentResponse(SentimentService sentiment, Random random)
        {
            _sentiment = sentiment;
            _random = random;
        }

        public async Task<IConversation> TryRespond(IMessage message, bool containsMention)
        {
            var s = await _sentiment.Sentiment(message.Content);

            if (s < Bracket)
                return new SentimentConversation(Sad.Random(_random));
            else if (s > (1 - Bracket))
                return new SentimentConversation(Happy.Random(_random));
            else
                return null;
        }

        private class SentimentConversation
            : TerminalConversation
        {
            public SentimentConversation([CanBeNull] params IEmote[] reactions)
                : base(null, reactions)
            {
            }
        }
    }
}
