using Discord;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Mute.Moe.Discord.Context;
using Mute.Moe.Extensions;
using Mute.Moe.Discord.Services.ImageGeneration;
using SixLabors.ImageSharp;
using Image = SixLabors.ImageSharp.Image;

namespace Mute.Moe.Services.ImageGen;

public interface IImageGenerator
{
    Task<IReadOnlyCollection<Image>> Text2Image(int? seed, string positive, string negative, Func<ProgressReport, Task>? progress = null, int batch = 1);

    Task<IReadOnlyCollection<Image>> Image2Image(int? seed, Image image, string positive, string negative, Func<ProgressReport, Task>? progress = null, int batch = 1);

    public record struct ProgressReport(float Progress, MemoryStream? Intermediate);
}

public static class IImageGeneratorExtensions
{
    private const string BaseNegative = "easynegative, badhandv4, bad-hands-5, logo, Watermark, username, signature, jpeg artifacts";

    private static async Task<(string, string)?> SetupPrompt(MuteCommandContext context, string prompt, string negative)
    {
        if (!negative.Contains(BaseNegative))
            negative = BaseNegative + ", " + negative;

        // If it's a public channel apply extra precautions
        if (!context.IsPrivate)
        {
            var ban = context.Services.GetRequiredService<IImageGeneratorBannedWords>();
            if (ban.IsBanned(prompt))
            {
                await context.Channel.SendMessageAsync("Sorry, I can't generate that image (use a DM channel to disable filters).");
                return null;
            }

            negative += ", (nsfw:1.4)";
        }

        return (prompt, negative);
    }

    /// <summary>
    /// Runs either text2image or image2image, depending upon if the context supplies any images.
    /// Images will be retrieved from this message, or the image it refers to.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="generator"></param>
    /// <param name="positive"></param>
    /// <param name="negative"></param>
    /// <param name="batchSize"></param>
    /// <returns></returns>
    public static async Task GenerateImage(this MuteCommandContext context, IImageGenerator generator, string positive, string negative, int batchSize)
    {
        var http = context.Services.GetRequiredService<HttpClient>();
        var random = context.Services.GetRequiredService<Random>();
        var seed = random.Next();

        // Load all of the source images we can find in the message
        var images = await context
            .Message
            .GetMessageImages(http)
            .ToAsyncEnumerable()
            .SelectMany(a => a.ToAsyncEnumerable())
            .ToReadOnlyListAsync();

        await context.GenerateImage(
            positive, negative,
            async (p, n, r) =>
            {
                if (images.Count == 0)
                    return await generator.Text2Image(seed, p, n, r, batchSize);

                using var image = await Image.LoadAsync(images.Random(random));
                return await generator.Image2Image(seed, image, p, n, r, batchSize);
            }
        );
    }

    public static async Task GenerateImage(this MuteCommandContext context, string positive, string negative, ImageGenerate generate)
    {
        var prompt = await SetupPrompt(context, positive, negative);
        if (prompt == null)
            return;
        (positive, negative) = prompt.Value;

        var reply = await context.Channel.SendMessageAsync(
            "Starting image generation...",
            allowedMentions: AllowedMentions.All,
            messageReference: new MessageReference(context.Message.Id)
        );

        // Do the generation
        try
        {
            var reporter = new ImageProgressReporter(reply, positive, negative);
            var result = await generate(positive, negative, reporter.ReportAsync);

            // Send a final result
            await reporter.FinalImages(result);
        }
        catch (Exception ex)
        {
            await reply.ModifyAsync(msg => msg.Content = $"Image generation failed!\n{ex.Message}");
        }
    }

    public delegate Task<IReadOnlyCollection<Image?>> ImageGenerate(string positive, string negative, Func<IImageGenerator.ProgressReport, Task> reportProgress);

    private class ImageProgressReporter
    {
        private readonly IUserMessage _message;

        private readonly string _positive;
        private readonly string _negative;

        private float _latestProgress = -1;

        public ImageProgressReporter(IUserMessage message, string positive, string negative)
        {
            _message = message;
            _positive = positive;
            _negative = negative;
        }

        public async Task ReportAsync(IImageGenerator.ProgressReport value)
        {
            _latestProgress = Math.Max(_latestProgress, value.Progress);

            await _message.ModifyAsync(props =>
            {
                props.Content = $"Generating ({_latestProgress:P0})";
            });
        }

        public async Task FinalImages(IEnumerable<Image?> images)
        {
            var attachments = new List<FileAttachment>();
            foreach (var image in images)
            {
                if (image == null)
                    continue;

                var mem = new MemoryStream();
                await image.SaveAsPngAsync(mem);
                mem.Position = 0;
                attachments.Add(new FileAttachment(mem, $"diffusion{attachments.Count}.png"));
            }

            await _message.ModifyAsync(props =>
            {
                props.Attachments = new Optional<IEnumerable<FileAttachment>>(attachments);
                props.Content = _positive;
                props.Components = MidjourneyStyleImageGenerationResponses.CreateButtons(attachments.Count).Build();
            });
        }
    }
}