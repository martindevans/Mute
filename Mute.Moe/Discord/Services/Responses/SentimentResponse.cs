using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;

using Mute.Moe.Discord.Context;
using Mute.Moe.Extensions;
using Mute.Moe.Services.Sentiment;
using Mute.Moe.Utilities;

namespace Mute.Moe.Discord.Services.Responses
{
    public class SentimentResponse
        : IResponse
    {
        private readonly Random _random;

        private double Bracket => _config.CertaintyThreshold;
        public double BaseChance => _config.ReactionChance;
        public double MentionedChance => _config.MentionReactionChance;

        public static readonly IReadOnlyList<string> Sad = new[] {
            EmojiLookup.BrokenHeart,
            EmojiLookup.ThumbsDown,
            EmojiLookup.Worried,
            EmojiLookup.SlightlyFrowning,
            EmojiLookup.Crying
        };
        public static readonly IReadOnlyList<string> Happy = new[] {
            EmojiLookup.Heart,
            EmojiLookup.ThumbsUp,
            EmojiLookup.Grin,
            EmojiLookup.Smile,
            EmojiLookup.SlightSmile
        };
        public static readonly IReadOnlyList<string> Neutral = new[] {
            EmojiLookup.Expressionless,
            EmojiLookup.Pensive,
            EmojiLookup.Confused
        };

        private readonly SentimentReactionConfig _config;

        public SentimentResponse(Configuration config, Random random)
        {
            _config = config.SentimentReactions ?? throw new ArgumentNullException(nameof(config.SentimentReactions));
            _random = random;
        }

        public async Task<IConversation?> TryRespond(MuteCommandContext context, bool containsMention)
        {
            var s = await context.Sentiment();

            if (s.ClassificationScore < Bracket || s.Classification == Sentiment.Neutral)
                return null;

            if (s.Classification == Sentiment.Positive)
                return new SentimentConversation(new Emoji(Happy.Random(_random)));
            else
                return new SentimentConversation(new Emoji(Sad.Random(_random)));
        }

        private class SentimentConversation
            : TerminalConversation
        {
            public SentimentConversation(params IEmote[]? reactions)
                : base(null, reactions)
            {
            }
        }
    }
}
