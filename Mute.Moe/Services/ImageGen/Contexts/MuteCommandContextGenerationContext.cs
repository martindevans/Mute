using System.Net.Http;
using System.Threading.Tasks;
using Discord;

namespace Mute.Moe.Services.ImageGen.Contexts;

public class MuteCommandContextGenerationContext
    : BaseImageGenerationContext
{
    private readonly IUserMessage _reply;

    /// <inheritdoc />
    protected override ulong ID => _reply.Id;

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