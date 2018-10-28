using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using JetBrains.Annotations;
using Mute.Extensions;

namespace Mute.Services.Responses
{
    public class SentimentResponse
        : IResponse
    {
        private readonly SentimentService _sentiment;
        private readonly Random _random;

        private const double Bracket = 0.95;

        public double BaseChance => 0.05;
        public double MentionedChance => 0.25;

        public static readonly IReadOnlyList<Emoji> Sad = new[] {
            EmojiLookup.BrokenHeart,
            EmojiLookup.ThumbsDown,
            EmojiLookup.Worried,
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
        public static readonly IReadOnlyList<Emoji> Neutral = new[] {
            EmojiLookup.Expressionless,
            EmojiLookup.Pensive,
            EmojiLookup.Confused
        };

        public SentimentResponse(SentimentService sentiment, Random random)
        {
            _sentiment = sentiment;
            _random = random;
        }

        public async Task<IConversation> TryRespond(ICommandContext context, bool containsMention)
        {
            var s = await _sentiment.Predict(context.Message.Content);

            if (s.Score < Bracket || s.Classification == SentimentService.Sentiment.Neutral)
                return null;

            if (s.Classification == SentimentService.Sentiment.Positive)
                return new SentimentConversation(Happy.Random(_random));
            else
                return new SentimentConversation(Sad.Random(_random));
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
