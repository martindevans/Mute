using System.Threading.Tasks;
using Discord.Commands;


namespace Mute.Moe.Discord.Context.Postprocessing;

/// <summary>
/// After every unsuccessfully executed command, all services registered to DI which implement this will be invoked
/// </summary>
public interface IUnsuccessfulCommandPostprocessor
{
    /// <summary>
    /// Postprocessors will be executed in order, sorted by this property
    /// </summary>
    uint Order { get; }

    /// <summary>
    /// Process the context of the unsuccessful command that just failed to run
    /// </summary>
    /// <param name="context"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    Task<bool> Process(MuteCommandContext context, IResult result);
}