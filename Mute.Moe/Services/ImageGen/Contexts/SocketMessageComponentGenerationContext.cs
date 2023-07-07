using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Mute.Moe.Services.ImageGen.Contexts;

public class SocketMessageComponentGenerationContext
    : BaseImageGenerationContext
{
    private readonly SocketMessageComponent _component;

    public SocketMessageComponentGenerationContext(ImageGenerationConfig config, IImageGenerator generator, IImageUpscaler upscaler, IImageOutpainter outpainter, HttpClient http, SocketMessageComponent component)
        : base(config, generator, upscaler, outpainter, http)
    {
        _component = component;
    }

    protected override async Task ModifyReply(Action<MessageProperties> modify)
    {
        await _component.ModifyOriginalResponseAsync(modify);
    }
}