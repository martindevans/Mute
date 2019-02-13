using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Mute.Moe.Services.Words
{
    public interface IWords
    {
        [NotNull, ItemCanBeNull] Task<IReadOnlyList<float>> Vector(string word);

        [NotNull, ItemCanBeNull] Task<IReadOnlyList<ISimilarWord>> Similar(string word);

        [NotNull] Task<double?> Similarity(string a, string b);
    }

    public interface ISimilarWord
    {
        string Word { get; }

        float Similarity { get; }
    }
}
