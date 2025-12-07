using System.Threading.Tasks;
using Discord.Commands;

namespace Mute.Moe.Discord.Context.Preprocessing;

/// <summary>
/// Preprocess a context for messages which have a command character prefix
/// </summary>
public interface ICommandPreprocessor
{
    /// <summary>
    /// Preprocess the context, before the command handler is invoked
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    Task Process(ICommandContext context);
}