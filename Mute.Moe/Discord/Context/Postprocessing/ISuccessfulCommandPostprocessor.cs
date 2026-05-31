namespace Mute.Moe.Discord.Context.Postprocessing;

/// <summary>
/// Postprocess a context for messages which have a command character prefix, after the command handler was invoked and returned successfully.
/// </summary>
public interface ISuccessfulCommandPostprocessor
    : IContextProcessor
{
}