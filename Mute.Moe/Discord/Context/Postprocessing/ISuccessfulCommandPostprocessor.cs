using System.Threading.Tasks;

namespace Mute.Moe.Discord.Context.Postprocessing
{
    public interface ISuccessfulCommandPostprocessor
    {
        Task Process(MuteCommandContext context);
    }
}
