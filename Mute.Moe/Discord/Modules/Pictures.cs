using Discord;
using Discord.Commands;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Services.ImageGen;
using Mute.Moe.Services.ImageGen.Contexts;
using Mute.Moe.Utilities;
using SixLabors.ImageSharp;
using System.Net.Http;
using System.Threading.Tasks;
using Image = SixLabors.ImageSharp.Image;

namespace Mute.Moe.Discord.Modules;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

[UsedImplicitly]
[Group("image")]
[ThinkingReply(EmojiLookup.ArtistPalette)]
public class Pictures(IImageAnalyser _analyser, HttpClient _http)
    : BaseModule
{
    [Command("generate"), Alias("diffusion", "imagine"), Summary("I will generate a picture")]
    [RateLimit("B05D7AF4-C797-45C9-93C9-062FDDA14760", 10, "Please wait a bit before generating more images")]
    [UsedImplicitly]
    public Task Generate([Remainder] string prompt)
    {
        return Context.GenerateImage(prompt);
    }


    [Command("metadata"), Alias("parameters"), Summary("I will try to extract stable diffusion generation data from the image")]
    [RateLimit("2E3E6C68-1862-4573-858A-B478000B8154", 5, "Please wait a bit")]
    [UsedImplicitly]
    public async Task Metadata()
    {
        var images = await GetMessageImages(Context.Message);
        if (images == null)
            return;

        var success = false;
        var count = 1;
        foreach (var img in images)
        {
            var meta = img.Metadata.GetPngMetadata();
            var parameters = meta.GetGenerationMetadata();

            if (parameters != null)
            {
                parameters = $"**Image {count++}**\n{parameters}\n";
                success = true;
                await LongReplyAsync(parameters);
            }

            await Task.Delay(450);
        }

        if (!success)
            await ReplyAsync("I couldn't find any metadata in any of those images");
    }


    [Command("analyse"), Alias("interrogate", "describe"), Summary("I will try to describe the image")]
    [RateLimit("B05D7AF4-C797-45C9-93C9-062FDDA14760", 30, "Please wait a bit before analysing more images")]
    [UsedImplicitly]
    public async Task Analyse()
    {
        var images = await GetMessageImages(Context.Message);
        if (images == null)
            return;

        var success = false;
        foreach (var stream in images)
        {
            var analysis = await _analyser.GetImageDescription(stream);
            var desc = analysis?.Description ?? "Something went wrong analysing that image";
            var title = analysis?.Title ?? "Image Analysis";

            success = true;

            var localTag = _analyser.IsLocal ? " (local)" : "";
            var embed = new EmbedBuilder()
                       .WithFooter($"🧠 {_analyser.ModelName}{localTag}")
                       .WithDescription(desc)
                       .WithTitle(title)
                       .Build();
            await ReplyAsync(embed: embed);

            await Task.Delay(250);
        }

        if (!success)
            await ReplyAsync("I couldn't find any metadata in any of those images");
    }
    

    private async Task<IReadOnlyList<Image>?> GetMessageImages(IUserMessage message)
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