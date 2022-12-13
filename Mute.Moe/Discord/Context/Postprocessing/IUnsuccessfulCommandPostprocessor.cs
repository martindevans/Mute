using System.Threading.Tasks;
using Discord.Commands;


namespace Mute.Moe.Discord.Context.Postprocessing;

public interface IUnsuccessfulCommandPostprocessor
{
    uint Order { get; }

    Task<bool> Process( MuteCommandContext context,  IResult result);
}