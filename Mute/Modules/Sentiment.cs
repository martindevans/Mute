using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Humanizer;
using JetBrains.Annotations;
using Mute.Extensions;
using Mute.Services;

namespace Mute.Modules
{
    public class Sentiment
        : BaseModule
    {
        private readonly DiscordSocketClient _client;
        private readonly ISentimentService _sentiment;
        private readonly SentimentReactionConfig _config;

        public Sentiment([NotNull] Configuration config, DiscordSocketClient client, ISentimentService sentiment)
        {
            _client = client;
            _sentiment = sentiment;
            _config = config.SentimentReactions;
        }

        [Command("sentiment"), Summary("I will show my opinion of a message")]
        public async Task AskSentiment([NotNull, Remainder] string message)
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

        private async Task ReactWithSentiment([NotNull] IUserMessage message, SentimentResult? score = null)
        {
            var result = score ?? await _sentiment.Predict(message.Content);
            if (result.ClassificationScore < _config.CertaintyThreshold)
                await message.AddReactionAsync(EmojiLookup.Confused);

            switch (result.Classification)
            {
                case Services.Sentiment.Positive:
                    await message.AddReactionAsync(EmojiLookup.ThumbsUp);
                    break;
                case Services.Sentiment.Neutral:
                    await message.AddReactionAsync(EmojiLookup.Expressionless);
                    break;
                case Services.Sentiment.Negative:
                    await message.AddReactionAsync(EmojiLookup.ThumbsDown);
                    break;
            }
        }

        [Command("sentiment-score"), Summary("I will show my opinion of a message numerically")]
        public async Task AskSentimentScore([NotNull, Remainder] string message)
        {
            await ShowSentimentScore(await _sentiment.Predict(message));
        }

        [Command("sentiment-score"), Summary("I will show my opinion of the previous message numerically")]
        public async Task AskSentimentScore(byte offset = 0)
        {
            var msg = await GetPreviousMessage(offset);
            if (msg == null)
                return;

            await ShowSentimentScore(await _sentiment.Predict(msg.Content), msg.Content);
        }

        private async Task ShowSentimentScore(SentimentResult score, [CanBeNull] string quote = null)
        {
            var embed = new EmbedBuilder()
                .WithTitle(quote)
                .AddField("Positive", $"{score.PositiveScore:#0.##}")
                .AddField("Neutral", $"{score.NeutralScore:#0.##}")
                .AddField("Negative", $"{score.NegativeScore:#0.##}")
                .AddField("Delay", score.ClassificationTime.Humanize(2));

            switch (score.Classification)
            {
                case Services.Sentiment.Negative:
                    embed = embed.WithColor(Color.Red);
                    break;
                case Services.Sentiment.Positive:
                    embed = embed.WithColor(Color.Blue);
                    break;
                case Services.Sentiment.Neutral:
                    embed = embed.WithColor(Color.DarkGrey);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            await ReplyAsync(embed);
        }

        [ItemCanBeNull] private async Task<IUserMessage> GetPreviousMessage(byte offset)
        {
            var messages = await Context.Channel.GetMessagesAsync(Context.Message, Direction.Before, byte.MaxValue).FlattenAsync();
            return messages.Skip(offset).FirstOrDefault() as IUserMessage;
        }

        [Command("mass-sentiment"), Summary("I will tag the last N messages with sentiment reactions"), RequireOwner]
        public async Task MassSentiment(byte count = 10)
        {
            await Context.Message.ThinkingReplyAsync(_client.CurrentUser, Task.Run(async () => {
                for (byte i = 0; i < count; i++)
                {
                    await AskSentiment(i);
                    await Task.Delay(250);
                }
            }));
        }
    }
}
