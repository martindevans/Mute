using System.Threading.Tasks;

namespace Mute.Moe.Discord.Context.Postprocessing;

/// <summary>
/// After every successfully executed command, all services registered to DI which implement this will be invoked
/// </summary>
public interface ISuccessfulCommandPostprocessor
{
    /// <summary>
    /// Process the context of the successful command that was just run
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    Task Process(MuteCommandContext context);
}