using System.Globalization;
using System.Threading.Tasks;
using Discord.Commands;
using JetBrains.Annotations;
using Mute.Services;

namespace Mute.Modules
{
    class Sentiment
        : ModuleBase
    {
        private readonly SentimentService _sentiment;

        public Sentiment(SentimentService sentiment)
        {
            _sentiment = sentiment;
        }

        [Command("sentiment")]
        public async Task AskSentiment([NotNull, Remainder] string message)
        {
            var result = await _sentiment.Sentiment(message);
            await ReplyAsync(result.ToString(CultureInfo.InvariantCulture));
        }

        [Command("sentiment-metrics")]
        public async Task SentimentMetrics()
        {
            var result = await _sentiment.EvaluateModelMetrics();
            await ReplyAsync("Accuracy: " + result.Accuracy.ToString(CultureInfo.InvariantCulture));
        }

        [RequireOwner, Command("sentiment-retrain")]
        public async Task SentimentTrain()
        {
            await _sentiment.ForceRetrain();
            await ReplyAsync("Retrained");
            await SentimentMetrics();
        }
    }
}
