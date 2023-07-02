using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Mute.Moe.Services.ImageGen.Contexts;

public class SocketMessageComponentGenerationContext
    : BaseImageGenerationContext
{
    private readonly SocketMessageComponent _component;

    public SocketMessageComponentGenerationContext(ImageGenerationConfig config, IImageGenerator generator, IImageUpscaler upscaler, HttpClient http, SocketMessageComponent component)
        : base(config, generator, upscaler, http)
    {
        _component = component;
    }

    protected override async Task ModifyReply(Action<MessageProperties> modify)
    {
        await _component.ModifyOriginalResponseAsync(modify);
    }
}