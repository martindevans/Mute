using System.Net.Http;
using System.Threading.Tasks;
using Discord;

namespace Mute.Moe.Services.ImageGen.Contexts;

public class MuteCommandContextGenerationContext
    : BaseImageGenerationContext
{
    private readonly IUserMessage _reply;

    protected override ulong ID => _reply.Id;

    public MuteCommandContextGenerationContext(ImageGenerationConfig config, IImageGenerator generator, IImageUpscaler upscaler, IImageOutpainter outpainter, HttpClient http, IUserMessage reply, IImageGenerationConfigStorage storage)
        : base(config, storage, generator, upscaler, outpainter, http)
    {
        _reply = reply;
    }

    protected override async Task ModifyReply(Action<MessageProperties> modify)
    {
        await _reply.ModifyAsync(modify);
    }
}