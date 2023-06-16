using System;
using System.IO;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using JetBrains.Annotations;
using Mute.Moe.Discord.Attributes;
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

    private readonly string[] _bannedWords =
    {
        "nsfw", "porn", "erotic", "fuck", "naked", "nude",
        "spider", "arachnid", "tarantula",
    };

    public Pictures(Random random, IImageGenerator generator, IImageAnalyser analyser, HttpClient http)
    {
        _random = random;
        _generator = generator;
        _analyser = analyser;
        _http = http;
    }

    [Command("generate"), Summary("I will generate a picture")]
    [RateLimit("B05D7AF4-C797-45C9-93C9-062FDDA14760", 15, "Please wait a bit before generating more images")]
    public async Task Generate([Remainder] string prompt)
    {
        var negative = "easynegative, badhandv4, logo, Watermark, username, signature, jpeg artifacts";

        // If it's a public channel apply extra precautions
        if (!Context.IsPrivate)
        {
            if (_bannedWords.Any(word => prompt.Contains(word, StringComparison.InvariantCultureIgnoreCase)))
            {
                await ReplyAsync("Sorry, I can't generate that image (use a DM channel to disable filters).");
                return;
            }

            negative += ", (nsfw:1.5)";
        }
        
        var reply = await ReplyAsync(
            "Starting image generation...",
            allowedMentions: AllowedMentions.All,
            messageReference: new MessageReference(Context.Message.Id)
        );

        try
        {
            // Do the generation
            var reporter = new ImageProgressReporter(reply);
            var result = await _generator.GenerateImage(_random.Next(), prompt, negative, reporter.ReportAsync);

            // Send a final result
            await reporter.FinalImage(result);
        }
        catch (Exception ex)
        {
            await reply.ModifyAsync(msg => msg.Content = $"Image generation failed!\n{ex.Message}");
        }
    }

    [Command("metadata"), Alias("parameters")]
    [RateLimit("2E3E6C68-1862-4573-858A-B478000B8154", 5, "Please wait a bit")]
    public async Task Metadata()
    {
        if (Context.Message.ReferencedMessage == null)
        {
            await ReplyAsync("Please use this command in a message which replies to another message");
            return;
        }

        if (Context.Message.ReferencedMessage.Attachments.Count == 0)
        {
            await ReplyAsync("That message doesn't seem to have any attachments");
            return;
        }

        var pngs = Context.Message.ReferencedMessage.Attachments.Where(a => a.ContentType == "image/png").ToList();
        if (pngs.Count == 0)
        {
            await ReplyAsync("That message doesn't seem to have any PNG attachments");
            return;
        }

        var success = false;
        foreach (var png in pngs)
        {
            var img = await SixLabors.ImageSharp.Image.IdentifyAsync(await _http.GetStreamAsync(png.Url));
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

    [Command("analyse"), Alias("interrogate", "clip")]
    [RateLimit("B05D7AF4-C797-45C9-93C9-062FDDA14760", 15, "Please wait a bit")]
    public async Task Analyse()
    {
        if (Context.Message.ReferencedMessage == null)
        {
            await ReplyAsync("Please use this command in a message which replies to another message");
            return;
        }

        if (Context.Message.ReferencedMessage.Attachments.Count == 0)
        {
            await ReplyAsync("That message doesn't seem to have any attachments");
            return;
        }

        var images = Context.Message.ReferencedMessage.Attachments.Where(a => a.ContentType.StartsWith("image/")).ToList();
        if (images.Count == 0)
        {
            await ReplyAsync("That message doesn't seem to have any image attachments");
            return;
        }

        var pngs = images.Select(GetPngStream);

        var success = false;
        foreach (var png in pngs)
        {
            var stream = await png;
            var description = await _analyser.GetImageDescription(stream);
            success = true;

            await ReplyAsync(description);
            await Task.Delay(250);
        }

        if (!success)
            await ReplyAsync("I couldn't find any metadata in any of those images");
    }

    private async Task<Stream> GetPngStream(IAttachment attachment)
    {
        var input = await _http.GetStreamAsync(attachment.Url);
        if (attachment.ContentType == "image/png")
            return input;

        var image = await Image.LoadAsync(input);

        var output = new MemoryStream();
        await image.SaveAsPngAsync(output);

        output.Position = 0;
        return output;
    }

    private class ImageProgressReporter
    {
        private readonly IUserMessage _message;

        public ImageProgressReporter(IUserMessage message)
        {
            _message = message;
        }

        public async Task ReportAsync(IImageGenerator.ProgressReport value)
        {
            //if (value.Intermediate != null)
            //{
            //    await _message.ModifyAsync(props =>
            //    {
            //        props.Attachments = new Optional<IEnumerable<FileAttachment>>(new[]
            //        {
            //            new FileAttachment(value.Intermediate, "WIP.png")
            //        });
            //    });

            //}
            //else
            {
                await _message.ModifyAsync(props =>
                {
                    props.Content = $"Generating ({value.Progress:P0})";
                });
            }
        }

        public async Task FinalImage(Stream image)
        {
            await _message.ModifyAsync(props =>
            {
                props.Attachments = new Optional<IEnumerable<FileAttachment>>(new[]
                {
                    new FileAttachment(image, "diffusion.png")
                });

                props.Content = "";
            });
        }
    }
}