using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Autofocus.Config;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using JetBrains.Annotations;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Extensions;
using Mute.Moe.Services.ImageGen;
using Mute.Moe.Utilities;
using SixLabors.ImageSharp;
using Image = SixLabors.ImageSharp.Image;

namespace Mute.Moe.Discord.Modules;

[UsedImplicitly]
[Group("image")]
[ThinkingReply(EmojiLookup.ArtistPalette)]
public class Pictures
    : BaseModule
{
    private readonly Random _random;
    private readonly IImageGenerator _generator;
    private readonly IImageAnalyser _analyser;
    private readonly HttpClient _http;

    public Pictures(Random random, IImageGenerator generator, IImageAnalyser analyser, HttpClient http)
    {
        _random = random;
        _generator = generator;
        _analyser = analyser;
        _http = http;
    }

    [Command("generate"), Alias("diffusion", "imagine"), Summary("I will generate a picture")]
    [RateLimit("B05D7AF4-C797-45C9-93C9-062FDDA14760", 15, "Please wait a bit before generating more images")]
    public async Task Generate([Remainder] string prompt)
    {
        await _generator.EasyGenerate(Context, prompt);
    }

    [Command("metadata"), Alias("parameters"), Summary("I will try to extract stable diffusion generation data from the image")]
    [RateLimit("2E3E6C68-1862-4573-858A-B478000B8154", 5, "Please wait a bit")]
    public async Task Metadata()
    {
        var images = await GetMessageImages(Context.Message);
        if (images == null)
            return;

        var success = false;
        foreach (var stream in images)
        {
            var img = await Image.IdentifyAsync(stream);
            var meta = img.Metadata.GetPngMetadata();
            var parameters = meta.TextData.FirstOrDefault(a => a.Keyword == "parameters");
            if (parameters.Value != null)
            {
                success = true;
                await ReplyAsync(parameters.Value);
            }

            await Task.Delay(250);
        }

        if (!success)
            await ReplyAsync("I couldn't find any metadata in any of those images");
    }

    [Command("analyse"), Alias("interrogate", "clip", "describe"), Summary("I will try to describe the image")]
    [RateLimit("B05D7AF4-C797-45C9-93C9-062FDDA14760", 15, "Please wait a bit")]
    public async Task Analyse(InterrogateModel model = InterrogateModel.CLIP)
    {
        var images = await GetMessageImages(Context.Message);
        if (images == null)
            return;

        var success = false;
        foreach (var stream in images)
        {
            var description = await _analyser.GetImageDescription(stream, model);
            success = true;

            await ReplyAsync(description);
            await Task.Delay(250);
        }

        if (!success)
            await ReplyAsync("I couldn't find any metadata in any of those images");
    }
    
    private async Task<IReadOnlyList<Stream>?> GetMessageImages(SocketUserMessage message, bool convertToPng = true)
    {
        var attachments = message.Attachments.ToList<IAttachment>();
        attachments.AddRange(message.ReferencedMessage?.Attachments ?? Array.Empty<IAttachment>());

        var result = await attachments
            .ToAsyncEnumerable()
            .Where(a => a.ContentType.StartsWith("image/"))
            .SelectAwait(async a => await a.GetPngStream(_http, convertToPng))
            .Where(a => a != null)
            .Select(a => a!)
            .ToListAsync();

        if (result.Count == 0)
        {
            await TypingReplyAsync("Please include some image attachments or reply to another message with image attachments!");
            return null;
        }

        return result;
    }
}