using System.Threading.Tasks;


namespace Mute.Moe.Discord.Context.Preprocessing;

/// <summary>
/// Preprocess every message received by the bot
/// </summary>
public interface IMessagePreprocessor
{
    /// <summary>
    /// Preprocess the context, before any commands or responses are generated
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    Task Process(MuteCommandContext context);
}