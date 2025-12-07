using System.Threading.Tasks;

namespace Mute.Moe.Discord.Context.Preprocessing;

/// <summary>
/// Preprocess messages which do not have a command character prefix
/// </summary>
public interface IConversationPreprocessor
{
    /// <summary>
    /// Preprocess the context, before the conversational response handler is invoked
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    Task Process(MuteCommandContext context);
}