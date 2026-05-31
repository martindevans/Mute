namespace Mute.Moe.Discord.Context.Preprocessing;

/// <summary>
/// Preprocess all messages which are <b>not</b> being interpreted as a command before
/// any conversational responses are generated.
/// </summary>
public interface IConversationPreprocessor
    : IContextProcessor
{
}