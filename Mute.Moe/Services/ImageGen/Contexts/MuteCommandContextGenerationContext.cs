using System.Net.Http;
using System.Threading.Tasks;
using Discord;

namespace Mute.Moe.Services.ImageGen.Contexts;

public class MuteCommandContextGenerationContext
    : BaseImageGenerationContext
{
    private readonly IUserMessage _reply;

    public MuteCommandContextGenerationContext(ImageGenerationConfig config, IImageGenerator generator, IImageUpscaler upscaler, HttpClient http, IUserMessage reply)
        : base(config, generator, upscaler, http)
    {
        _reply = reply;
    }

    protected override async Task ModifyReply(Action<MessageProperties> modify)
    {
        await _reply.ModifyAsync(modify);
    }
}