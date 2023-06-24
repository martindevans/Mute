using System.Threading.Tasks;
using Mute.Moe.Services.Host;

namespace Mute.Moe.Services.Sentiment;

public interface ISentimentEvaluator
    : IHostedService
{
    Task<SentimentResult> Predict(string message);
}

public enum Sentiment
{
    Negative = 0,
    Positive = 1,
    Neutral = 2,
}

public readonly struct SentimentResult
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

        if (ClassificationScore == 0)
        {
            Classification = Sentiment.Neutral;
        }
        else
        {
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