using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Sentiment
{
    public class NullSentiment
        : ISentimentEvaluator
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<SentimentResult> Predict(string message)
        {
            return Task.FromResult(new SentimentResult(message, 0, 0, 0, TimeSpan.Zero));
        }
    }
}
