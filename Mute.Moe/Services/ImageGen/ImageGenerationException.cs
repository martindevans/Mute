namespace Mute.Moe.Services.ImageGen;

/// <summary>
/// Base class to image generation exception
/// </summary>
/// <param name="message"></param>
public abstract class ImageGenerationException(string message)
    : Exception(message);

/// <summary>
/// Requested generation must be done in a private or NSFW channel
/// </summary>
public class ImageGenerationPrivateChannelRequiredException()
    : ImageGenerationException("I'm sorry I can't generate that image (use a DM channel to disable filters).");