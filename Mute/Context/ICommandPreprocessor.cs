using Discord.Commands;

namespace Mute.Context
{
    public interface ICommandPreprocessor
    {
        void Process(ICommandContext context);
    }
}
