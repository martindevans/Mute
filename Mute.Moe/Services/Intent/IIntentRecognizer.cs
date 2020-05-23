using System.Threading.Tasks;
using Discord.Commands;


namespace Mute.Moe.Services.Intent
{
    public interface IIntentRecognizer
    {
        Task<IIntentResult?> Recognize(string sentence);
    }

    public interface IIntentResult
    {
        CommandInfo Command { get; }

        ParseResult Arguments { get; }
    }
}
