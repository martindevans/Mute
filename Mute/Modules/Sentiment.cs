using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using JetBrains.Annotations;
using Mute.Services;

namespace Mute.Modules
{
    public class Sentiment
        : BaseModule
    {
        private readonly SentimentService _sentiment;
        private readonly DiscordSocketClient _client;

        public Sentiment(SentimentService sentiment, DiscordSocketClient client)
        {
            _sentiment = sentiment;
            _client = client;
        }

        [Command("sentiment"), Summary("I will show my opinion of a message")]
        public async Task AskSentiment([NotNull, Remainder] string message)
        {
            await ReactWithSentiment(Context.Message, await _sentiment.Predict(message));
        }

        [Command("sentiment"), Summary("I will show my opinion of the previous message")]
        public async Task AskSentiment(byte offset = 0)
        {
            var msg = await GetPreviousMessage(offset);
            if (msg == null)
                return;

            await ReactWithSentiment(msg);
        }

        private async Task ReactWithSentiment([NotNull] IUserMessage message, SentimentService.SentimentResult? score = null)
        {
            var result = score ?? await _sentiment.Predict(message.Content);
            if (result.Score < 0.75)
                await message.AddReactionAsync(EmojiLookup.Confused);

            switch (result.Classification)
            {
                case SentimentService.Sentiment.Positive:
                    await message.AddReactionAsync(EmojiLookup.ThumbsUp);
                    break;
                case SentimentService.Sentiment.Neutral:
                    await message.AddReactionAsync(EmojiLookup.Expressionless);
                    break;
                case SentimentService.Sentiment.Negative:
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

        private async Task ShowSentimentScore(SentimentService.SentimentResult score, [CanBeNull] string quote = null)
        {
            var embed = new EmbedBuilder()
                .WithTitle(quote)
                .AddField("Positive", $"{score.PositiveScore:#0.##}")
                .AddField("Neutral", $"{score.NeutralScore:#0.##}")
                .AddField("Negative", $"{score.NegativeScore:#0.##}");

            switch (score.Classification)
            {
                case SentimentService.Sentiment.Negative:
                    embed = embed.WithColor(Color.Red);
                    break;
                case SentimentService.Sentiment.Positive:
                    embed = embed.WithColor(Color.Blue);
                    break;
                case SentimentService.Sentiment.Neutral:
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

        //[Command("sentiment-metrics"), Summary("I will show statistics on the accuracy of my opinion")]
        //public async Task SentimentMetrics()
        //{
        //    var metrics = await Context.Message.ThinkingReplyAsync(_client.CurrentUser, _sentiment.EvaluateModelMetrics());

        //    //Type out the model metrics
        //    await ReplyAsync(
        //        $"```Micro Accuracy: {metrics.AccuracyMicro}\n" +
        //        $"Macro Accuracy: {metrics.AccuracyMacro}\n" +
        //        $"Log Loss: {metrics.LogLoss}\n" +
        //        $"Log Loss Reduction: {metrics.LogLossReduction}```"
        //    );
        //}

        //[RequireOwner, Command("sentiment-retrain"), Summary("I will retrain the ML models for sentiment analysis")]
        //public async Task SentimentRetrain()
        //{
        //    var w = new System.Diagnostics.Stopwatch();
        //    w.Start();

        //    await Context.Message.ThinkingReplyAsync(_client.CurrentUser, _sentiment.ForceRetrain());

        //    await TypingReplyAsync($"Retrained in {w.Elapsed.Humanize(precision:2)}");
        //    await SentimentMetrics();
        //}
    }
}
