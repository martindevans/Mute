using Discord.Commands;

namespace Mute.Moe.Discord.Context
{
    /// <summary>
    /// Preprocess a context for messages which have a command character prefix
    /// </summary>
    public interface ICommandPreprocessor
    {
        void Process(ICommandContext context);
    }
}
