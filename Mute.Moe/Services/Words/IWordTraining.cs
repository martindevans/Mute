using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Mute.Moe.Services.Words
{
    public interface IWordTraining
    {
        Task Train([NotNull] string word, [NotNull] string exampleSentence);
    }
}
