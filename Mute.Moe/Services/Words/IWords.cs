using System.Collections.Generic;
using System.Threading.Tasks;


namespace Mute.Moe.Services.Words
{
    public interface IWords
    {
        Task<IReadOnlyList<float>?> Vector(string word);

        Task<IReadOnlyList<ISimilarWord>?> Similar(string word);

         Task<double?> Similarity(string a, string b);
    }

    public interface ISimilarWord
    {
        string Word { get; }

        float Similarity { get; }
    }
}
