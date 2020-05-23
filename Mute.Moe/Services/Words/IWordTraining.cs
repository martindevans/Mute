using System.Threading.Tasks;


namespace Mute.Moe.Services.Words
{
    public interface IWordTraining
    {
        Task Train( string word,  string exampleSentence);
    }
}
