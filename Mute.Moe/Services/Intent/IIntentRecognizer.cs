using System.Threading.Tasks;
using Discord.Commands;
using JetBrains.Annotations;

namespace Mute.Moe.Services.Intent
{
    public interface IIntentRecognizer
    {
        [NotNull, ItemCanBeNull] Task<IIntentResult> Recognize(string sentence);
    }

    public interface IIntentResult
    {
        CommandInfo Command { get; }

        ParseResult Arguments { get; }
    }
}
