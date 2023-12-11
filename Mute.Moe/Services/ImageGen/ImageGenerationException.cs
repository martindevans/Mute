namespace Mute.Moe.Services.ImageGen;

public abstract class ImageGenerationException(string message)
    : Exception(message);

public class ImageGenerationPrivateChannelRequiredException()
    : ImageGenerationException("I'm sorry I can't generate that image (use a DM channel to disable filters).");