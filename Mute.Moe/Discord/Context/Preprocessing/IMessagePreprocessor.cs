namespace Mute.Moe.Discord.Context.Preprocessing;

/// <summary>
/// Preprocess every message received by the bot, before any other processing is applied or
/// any responses are generated.
/// </summary>
public interface IMessagePreprocessor
    : IContextProcessor
{
}