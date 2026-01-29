using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Mute.Moe.Services.ImageGen.Contexts;

/// <summary>
/// Context for image generation attached to a particular message component
/// </summary>
public class SocketMessageComponentGenerationContext
    : BaseImageGenerationContext
{
    private readonly SocketMessageComponent _component;

    /// <inheritdoc />
    protected override ulong ID => _component.Id;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="config"></param>
    /// <param name="generator"></param>
    /// <param name="upscaler"></param>
    /// <param name="outpainter"></param>
    /// <param name="http"></param>
    /// <param name="component"></param>
    /// <param name="storage"></param>
    public SocketMessageComponentGenerationContext(ImageGenerationConfig config, IImageGenerator generator, IImageUpscaler upscaler, IImageOutpainter outpainter, HttpClient http, SocketMessageComponent component, IImageGenerationConfigStorage storage)
        : base(config, storage, generator, upscaler, outpainter, http)
    {
        _component = component;
    }

    /// <inheritdoc />
    protected override async Task ModifyReply(Action<MessageProperties> modify)
    {
        await _component.ModifyOriginalResponseAsync(modify);
    }
}