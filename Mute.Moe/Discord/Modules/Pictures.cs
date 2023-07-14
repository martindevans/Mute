using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Autofocus.Config;
using Discord;
using Discord.Commands;
using JetBrains.Annotations;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Extensions;
using Mute.Moe.Services.ImageGen;
using Mute.Moe.Services.ImageGen.Contexts;
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
    private readonly IImageAnalyser _analyser;
    private readonly HttpClient _http;
    private readonly StableDiffusionBackendCache _backends;

    public Pictures(IImageAnalyser analyser, HttpClient http, StableDiffusionBackendCache backends)
    {
        _analyser = analyser;
        _http = http;
        _backends = backends;
    }

    [Command("backends"), Alias("status"), Summary("I will show the status of image generation backends")]
    public async Task Backends()
    {
        await BackendsCheck();
    }

    [Command("backends"), Alias("status"), Summary("I will show the status of image generation backends")]
    [RequireOwner]
    public async Task BackendsCheck(bool check = false)
    {
        if (!check)
            await _backends.GetBackend();

        var backends = await _backends.GetBackends(check);

        await DisplayItemList(
            backends,
            () => "No backends available",
            null,
            (item, index) => $"{index + 1}. `{item.Item1}` has been used {item.Item2} times, ({(item.Item3 ? "Ok" : "Unresponsive")})"
        );
    }

    [Command("generate"), Alias("diffusion", "imagine"), Summary("I will generate a picture")]
    [RateLimit("B05D7AF4-C797-45C9-93C9-062FDDA14760", 10, "Please wait a bit before generating more images")]
    public async Task Generate([Remainder] string prompt)
    {
        await Context.GenerateImage(prompt);
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
            var parameters = meta.GetGenerationMetadata();

            if (parameters != null)
            {
                success = true;
                await ReplyAsync(parameters);
            }

            await Task.Delay(450);
        }

        if (!success)
            await ReplyAsync("I couldn't find any metadata in any of those images");
    }

    [Command("analyse"), Alias("interrogate", "describe"), Summary("I will try to describe the image")]
    [RateLimit("B05D7AF4-C797-45C9-93C9-062FDDA14760", 30, "Please wait a bit")]
    public async Task Analyse(InterrogateModel model = InterrogateModel.DeepDanbooru)
    {
        var images = await GetMessageImages(Context.Message);
        if (images == null)
            return;

        var success = false;
        foreach (var stream in images)
        {
            var description = await _analyser.GetImageDescription(stream, model);
            success = true;

            await ReplyAsync($"```{description}```");
            await Task.Delay(250);
        }

        if (!success)
            await ReplyAsync("I couldn't find any metadata in any of those images");
    }
    
    private async Task<IReadOnlyList<Stream>?> GetMessageImages(IUserMessage message)
    {
        var result = await message.GetMessageImages(_http);
        if (result.Count == 0)
        {
            await TypingReplyAsync("Please include some image attachments or reply to another message with image attachments!");
            return null;
        }

        return result;
    }
}