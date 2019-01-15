using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Mute.Context;

namespace Mute.Services
{
    public static class MuteCommandContextSentimentExtension
    {
        public static async Task<SentimentResult> Sentiment([NotNull] this MuteCommandContext context)
        {
            var r = await context.GetOrAdd<SentimentResultContainer>(async () => {
                var sentiment = (ISentimentService)context.Services.GetService(typeof(ISentimentService));
                return new SentimentResultContainer(await sentiment.Predict(context.Message.Content));
            });

            return r.Result;
        }

        private class SentimentResultContainer
        {
            public readonly SentimentResult Result;

            public SentimentResultContainer(SentimentResult result)
            {
                Result = result;
            }
        }
    }

    public interface ISentimentService
    {
        Task<SentimentResult> Predict(string message);
    }

    public enum Sentiment
    {
        Negative = 0,
        Positive = 1,
        Neutral = 2
    }

    public struct SentimentResult
    {
        public string Text { get; }

        public float ClassificationScore { get; }
        public Sentiment Classification { get; }

        public float NegativeScore { get; }
        public float PositiveScore { get; }
        public float NeutralScore { get; }

        public TimeSpan ClassificationTime { get; }

        public SentimentResult(string message, float negative, float positive, float neutral, TimeSpan time)
        {
            Text = message;
            NegativeScore = negative;
            PositiveScore = positive;
            NeutralScore = neutral;
            ClassificationTime = time;

            ClassificationScore = Math.Max(negative, Math.Max(positive, neutral));

            // ReSharper disable CompareOfFloatsByEqualityOperator
            if (ClassificationScore == negative)
                Classification = Sentiment.Negative;
            else if (ClassificationScore == positive)
                Classification = Sentiment.Positive;
            else
                Classification = Sentiment.Neutral;
            // ReSharper restore CompareOfFloatsByEqualityOperator
        }
    }
}
