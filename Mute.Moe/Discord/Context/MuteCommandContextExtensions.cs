using System.Threading.Tasks;

using Mute.Moe.Services.Sentiment;

namespace Mute.Moe.Discord.Context
{
    public static class MuteCommandContextExtensions
    {
        public static async Task<SentimentResult> Sentiment( this MuteCommandContext context)
        {
            var r = await context.GetOrAdd(async () => {
                var sentiment = (ISentimentEvaluator)context.Services.GetService(typeof(ISentimentEvaluator));
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
}
