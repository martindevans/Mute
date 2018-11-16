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

        private double Bracket => _config.CertaintyThreshold;
        public double BaseChance => _config.ReactionChance;
        public double MentionedChance => _config.MentionReactionChance;

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

        private readonly SentimentReactionConfig _config;

        public SentimentResponse([NotNull] Configuration config, [NotNull] SentimentService sentiment, [NotNull] Random random)
        {
            _config = config.SentimentReactions;
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
