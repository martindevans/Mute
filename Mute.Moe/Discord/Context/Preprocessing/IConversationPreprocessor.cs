using System.Threading.Tasks;

namespace Mute.Moe.Discord.Context.Preprocessing;

/// <summary>
/// Preprocess message which do not have a command character prefix
/// </summary>
public interface IConversationPreprocessor
{
    Task Process(MuteCommandContext context);
}