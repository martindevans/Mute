using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Mute.Moe.Services.Sentiment;

namespace Mute.Moe.Discord.Context;

public static class MuteCommandContextExtensions
{
    public static async Task<SentimentResult> Sentiment(this MuteCommandContext context)
    {
        var r = await context.GetOrAdd(async () =>
        {
            var sentiment = context.Services.GetRequiredService<ISentimentEvaluator>();
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