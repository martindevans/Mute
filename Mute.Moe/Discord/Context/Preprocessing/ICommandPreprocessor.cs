using System.Threading.Tasks;
using Discord.Commands;

namespace Mute.Moe.Discord.Context.Preprocessing
{
    /// <summary>
    /// Preprocess a context for messages which have a command character prefix
    /// </summary>
    public interface ICommandPreprocessor
    {
        Task Process(ICommandContext context);
    }
}
