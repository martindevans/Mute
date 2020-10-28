using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Humanizer;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Discord.Context;
using Mute.Moe.Services.Sentiment;
using Mute.Moe.Utilities;

namespace Mute.Moe.Discord.Modules
{
    public class Sentiment
        : BaseModule
    {
        private readonly ISentimentEvaluator _sentiment;
        private readonly SentimentReactionConfig _config;

        public Sentiment(Configuration config, ISentimentEvaluator sentiment)
        {
            _sentiment = sentiment;
            _config = config.SentimentReactions ?? throw new ArgumentNullException(nameof(config.SentimentReactions));
        }

        [Command("sentiment"), Summary("I will show my opinion of a message")]
        public async Task AskSentiment([Remainder] string message)
        {
            await ReactWithSentiment(Context.Message, await Context.Sentiment());
        }

        [Command("sentiment"), Summary("I will show my opinion of the previous message")]
        public async Task AskSentiment(byte offset = 0)
        {
            var msg = await GetPreviousMessage(offset);
            if (msg == null)
                return;

            await ReactWithSentiment(msg);
        }

        private async Task ReactWithSentiment(IMessage message, SentimentResult? score = null)
        {
            var result = score ?? await _sentiment.Predict(message.Content);
            if (result.ClassificationScore < _config.CertaintyThreshold)
                await message.AddReactionAsync(new Emoji(EmojiLookup.Confused));

            switch (result.Classification)
            {
                case Moe.Services.Sentiment.Sentiment.Positive:
                    await message.AddReactionAsync(new Emoji(EmojiLookup.ThumbsUp));
                    break;
                case Moe.Services.Sentiment.Sentiment.Neutral:
                    await message.AddReactionAsync(new Emoji(EmojiLookup.Expressionless));
                    break;
                case Moe.Services.Sentiment.Sentiment.Negative:
                    await message.AddReactionAsync(new Emoji(EmojiLookup.ThumbsDown));
                    break;
            }
        }

        [Command("sentiment-score"), Summary("I will show my opinion of a message numerically")]
        public async Task AskSentimentScore([Remainder] string message)
        {
            await ShowSentimentScore(await Context.Sentiment());
        }

        [Command("sentiment-score"), Summary("I will show my opinion of the previous message numerically")]
        public async Task AskSentimentScore(byte offset = 0)
        {
            var msg = await GetPreviousMessage(offset);
            if (msg == null)
                return;

            await ShowSentimentScore(await _sentiment.Predict(msg.Content), msg.Content);
        }

        private async Task ShowSentimentScore(SentimentResult score, string? quote = null)
        {
            var embed = new EmbedBuilder()
                .WithTitle(quote)
                .AddField("Positive", $"{score.PositiveScore:#0.##}")
                .AddField("Neutral", $"{score.NeutralScore:#0.##}")
                .AddField("Negative", $"{score.NegativeScore:#0.##}")
                .AddField("Delay", score.ClassificationTime.Humanize(2));

            embed = score.Classification switch {
                Moe.Services.Sentiment.Sentiment.Negative => embed.WithColor(Color.Red),
                Moe.Services.Sentiment.Sentiment.Positive => embed.WithColor(Color.Blue),
                Moe.Services.Sentiment.Sentiment.Neutral => embed.WithColor(Color.DarkGrey),
                _ => throw new ArgumentOutOfRangeException()
            };

            await ReplyAsync(embed);
        }

        private async Task<IUserMessage?> GetPreviousMessage(byte offset)
        {
            var messages = await Context.Channel.GetMessagesAsync(Context.Message, Direction.Before, byte.MaxValue).FlattenAsync();
            return messages.Skip(offset).FirstOrDefault() as IUserMessage;
        }

        [Command("mass-sentiment"), Summary("I will tag the last N messages with sentiment reactions"), RequireOwner]
        [ThinkingReply, TypingReply]
        public async Task MassSentiment(byte count = 10)
        {
            for (byte i = 0; i < count; i++)
            {
                await AskSentiment(i);
                await Task.Delay(250);
            }
        }
    }
}
