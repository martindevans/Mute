using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Mute.Moe.Services.ImageGen.Contexts;

public class SocketMessageComponentGenerationContext
    : BaseImageGenerationContext
{
    private readonly SocketMessageComponent _component;

    protected override ulong ID => _component.Id;

    public SocketMessageComponentGenerationContext(ImageGenerationConfig config, IImageGenerator generator, IImageUpscaler upscaler, IImageOutpainter outpainter, HttpClient http, SocketMessageComponent component, IImageGenerationConfigStorage storage)
        : base(config, storage, generator, upscaler, outpainter, http)
    {
        _component = component;
    }

    protected override async Task ModifyReply(Action<MessageProperties> modify)
    {
        await _component.ModifyOriginalResponseAsync(modify);
    }
}