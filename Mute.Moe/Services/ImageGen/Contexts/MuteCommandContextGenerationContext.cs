using System.Net.Http;
using System.Threading.Tasks;
using Discord;

namespace Mute.Moe.Services.ImageGen.Contexts;

/// <summary>
/// Image generation context for an <see cref="IUserMessage"/>
/// </summary>
public class MuteCommandContextGenerationContext
    : BaseImageGenerationContext
{
    private readonly IUserMessage _reply;

    /// <inheritdoc />
    protected override ulong ID => _reply.Id;

    /// <summary>
    /// Create context for a particular message
    /// </summary>
    /// <param name="config"></param>
    /// <param name="generator"></param>
    /// <param name="upscaler"></param>
    /// <param name="outpainter"></param>
    /// <param name="http"></param>
    /// <param name="reply"></param>
    /// <param name="storage"></param>
    public MuteCommandContextGenerationContext(ImageGenerationConfig config, IImageGenerator generator, IImageUpscaler upscaler, IImageOutpainter outpainter, HttpClient http, IUserMessage reply, IImageGenerationConfigStorage storage)
        : base(config, storage, generator, upscaler, outpainter, http)
    {
        _reply = reply;
    }

    /// <inheritdoc />
    protected override Task ModifyReply(Action<MessageProperties> modify)
    {
        return _reply.ModifyAsync(modify);
    }
}