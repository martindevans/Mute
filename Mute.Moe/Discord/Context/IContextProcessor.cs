using System.Threading.Tasks;

namespace Mute.Moe.Discord.Context;

/// <summary>
/// Base interface for all context processors
/// </summary>
public interface IContextProcessor
{
    /// <summary>
    /// Execute this processor
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    Task Process(MuteCommandContext context);
}