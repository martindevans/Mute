using System.Threading.Tasks;
using Discord.Commands;

namespace Mute.Moe.Discord.Context.Postprocessing
{
    public interface IUnsuccessfulCommandPostprocessor
    {
        Task Process(MuteCommandContext context, IResult result);
    }
}
