using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Humanizer;
using JetBrains.Annotations;
using Microsoft.ML.Models;
using Mute.Extensions;
using Mute.Services;

namespace Mute.Modules
{
    public class Sentiment
        : ModuleBase
    {
        private readonly SentimentService _sentiment;
        private readonly DiscordSocketClient _client;

        public Sentiment(SentimentService sentiment, DiscordSocketClient client)
        {
            _sentiment = sentiment;
            _client = client;
        }

        [Command("sentiment")]
        public async Task AskSentiment([NotNull, Remainder] string message)
        {
            var result = await _sentiment.Sentiment(message);

            await Context.Message.AddReactionAsync(result > 0 ? EmojiLookup.ThumbsUp : EmojiLookup.ThumbsDown);

            if (Math.Abs(result) < 0.5f)
                await Context.Message.AddReactionAsync(EmojiLookup.Confused);
        }

        [Command("sentiment-score")]
        public async Task AskSentimentScore([NotNull, Remainder] string message)
        {
            var result = await _sentiment.Sentiment(message);
            await this.TypingReplyAsync(result.ToString("#0.##"));
        }

        [Command("sentiment-metrics")]
        public async Task SentimentMetrics()
        {
            BinaryClassificationMetrics result;
            using (Context.Channel.EnterTypingState())
            {
                //Add a thinking emoji and then evaluate the model
                var emoji = Context.Message.AddReactionAsync(EmojiLookup.Thinking);
                result = await _sentiment.EvaluateModelMetrics();
                await emoji;

                //Remove the thinking emoji
                await Context.Message.RemoveReactionAsync(EmojiLookup.Thinking, _client.CurrentUser);
            }

            //Type out the model metrics
            await ReplyAsync(
                $"```Accuracy: {result.Accuracy}\n" +
                $"Negative Precision: {result.NegativePrecision}\n" +
                $"Positive Precision: {result.PositivePrecision}\n" +
                $"Log Loss Reduction: {result.LogLossReduction}```"
            );
        }

        [RequireOwner, Command("sentiment-retrain")]
        public async Task SentimentRetrain()
        {
            var w = new System.Diagnostics.Stopwatch();
            w.Start();

            //Add a thinking emoji and then retrain the model
            var emoji = Context.Message.AddReactionAsync(EmojiLookup.Thinking);
            await _sentiment.ForceRetrain();
            await emoji;

            await this.TypingReplyAsync($"Retrained in {w.Elapsed.Humanize(precision:2)}");
            await SentimentMetrics();
        }
    }
}
