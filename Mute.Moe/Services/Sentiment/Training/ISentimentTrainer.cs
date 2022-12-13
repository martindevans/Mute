using System.Threading.Tasks;


namespace Mute.Moe.Services.Sentiment.Training;

public interface ISentimentTrainer
{
    Task Teach( string text, Sentiment sentiment);
}