using System.Threading.Tasks;

namespace Mute.Moe.Discord.Context.Preprocessing
{
    public interface IConversationPreprocessor
    {
        Task Process(MuteCommandContext context);
    }
}
