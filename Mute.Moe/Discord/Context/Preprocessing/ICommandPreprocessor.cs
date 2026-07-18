namespace Mute.Moe.Discord.Context.Preprocessing;

/// <summary>
/// Preprocess a context for messages which have a command character prefix, before the command handler is invoked.
/// </summary>
public interface ICommandPreprocessor
    : IContextProcessor;