namespace Mute.Moe.Services.ImageGen;

public abstract class ImageGenerationException
    : Exception
{
    protected ImageGenerationException(string message)
        : base(message)
    {
    }
}

public class ImageGenerationPrivateChannelRequiredException
    : ImageGenerationException
{
    public ImageGenerationPrivateChannelRequiredException()
        : base("I'm sorry I can't generate that image (use a DM channel to disable filters).")
    {
    }
}