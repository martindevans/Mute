namespace Mute.Moe.Discord.Context.Preprocessing;

/// <summary>
/// Preprocess all messages which do are <b>not</b> being interpreted as a command. Before
/// any conversational responses are generated.
/// </summary>
public interface IConversationPreprocessor
    : IContextProcessor
{
}