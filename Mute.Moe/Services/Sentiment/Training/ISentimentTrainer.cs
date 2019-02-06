using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Mute.Moe.Services.Sentiment.Training
{
    public interface ISentimentTrainer
    {
        Task Teach([NotNull] string text, Sentiment sentiment);
    }
}
