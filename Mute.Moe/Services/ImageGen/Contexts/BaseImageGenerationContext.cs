using System.Text;
using Mute.Moe.Discord.Context;
using System.Threading.Tasks;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using MoreLinq;
using Mute.Moe.Extensions;
using SixLabors.ImageSharp;
using System.IO;
using System.Net.Http;
using Mute.Moe.Discord.Services.ImageGeneration;
using Image = SixLabors.ImageSharp.Image;

namespace Mute.Moe.Services.ImageGen.Contexts;

public abstract class BaseImageGenerationContext
{
    private readonly ImageGenerationConfig _config;

    private readonly IImageGenerator _generator;
    private readonly IImageUpscaler _upscaler;
    private readonly IImageOutpainter _outpainter;
    private readonly HttpClient _http;

    private float _latestProgress;

    protected BaseImageGenerationContext(ImageGenerationConfig config, IImageGenerator generator, IImageUpscaler upscaler, IImageOutpainter outpainter, HttpClient http)
    {
        _config = config;
        _generator = generator;
        _upscaler = upscaler;
        _http = http;
        _outpainter = outpainter;
    }

    public async Task Run()
    {
        try
        {
            await OnStartingGeneration();
            var result = await Generate();
            await OnCompleted(_config.Positive, _config.Negative, result);
        }
        catch (Exception ex)
        {
            await OnFailed(ex);
        }
    }

    #region event callbacks
    protected abstract Task ModifyReply(Action<MessageProperties> modify);

    protected virtual Task OnStartingGeneration()
    {
        return Task.CompletedTask;
    }

    private async Task OnReportProgress(IImageGenerator.ProgressReport progressReport)
    {
        _latestProgress = Math.Max(_latestProgress, progressReport.Progress);

        await ModifyReply(props =>
        {
            props.Content = $"Generating ({_latestProgress:P0})";
        });
    }

    private async Task OnFailed(Exception exception)
    {
        await ModifyReply(msg => msg.Content = $"Image generation failed!\n{exception.Message}");
    }

    private async Task OnCompleted(string positive, string negative, IReadOnlyCollection<Image?> images)
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

        await ModifyReply(props => 
        {
            props.Attachments = new Optional<IEnumerable<FileAttachment>>(attachments);
            props.Content = positive + " **NOT** " + negative;
            props.Components = CreateButtons(attachments.Count).Build();
        });
    }
    #endregion

    #region generation
    protected async Task<IReadOnlyCollection<Image?>> Generate()
    {
        Image? referenceImage = null;
        if (_config.ReferenceImageUrl != null)
            referenceImage = await Image.LoadAsync(await _http.GetStreamAsync(_config.ReferenceImageUrl));

        if (referenceImage == null)
            return await GenerateText2Image();

        using (referenceImage)
        {
            return _config.Type switch
            {
                ImageGenerationType.Generate => await GenerateImage2Image(referenceImage),
                ImageGenerationType.Upscale => new[] { await GenerateUpscale(referenceImage) },
                ImageGenerationType.Outpaint => await GenerateOutpaint(referenceImage),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    private Task<Image> GenerateUpscale(Image referenceImage)
    {
        return _upscaler.UpscaleImage(referenceImage, (uint)referenceImage.Width * 2, (uint)referenceImage.Height * 2, OnReportProgress);
    }

    private Task<IReadOnlyCollection<Image>> GenerateImage2Image(Image referenceImage)
    {
        var prompt = _config.Prompt();
        return _generator.Image2Image(null, referenceImage, prompt, OnReportProgress, _config.BatchSize);
    }

    private Task<IReadOnlyCollection<Image>> GenerateText2Image()
    {
        var prompt = _config.Prompt();
        return _generator.Text2Image(null, prompt, OnReportProgress, _config.BatchSize);
    }

    private Task<IReadOnlyCollection<Image>> GenerateOutpaint(Image referenceImage)
    {
        return _outpainter.Outpaint(referenceImage, _config.Positive, _config.Negative, OnReportProgress);
    }
    #endregion

    #region send message with images
    private static ComponentBuilder CreateButtons(int count)
    {
        var upscaleRow = new ActionRowBuilder();
        var variantRow = new ActionRowBuilder();
        var outpaintRow = new ActionRowBuilder();
        for (var i = 0; i < count; i++)
        {
            upscaleRow.AddComponent(ButtonBuilder.CreatePrimaryButton($"U{i + 1}", MidjourneyStyleImageGenerationResponses.GetUpscaleButtonId(i)).Build());
            variantRow.AddComponent(ButtonBuilder.CreateSuccessButton($"V{i + 1}", MidjourneyStyleImageGenerationResponses.GetVariantButtonId(i)).Build());
            outpaintRow.AddComponent(ButtonBuilder.CreateDangerButton($"O{i + 1}", MidjourneyStyleImageGenerationResponses.GetOutpaintButtonId(i)).Build());
        }

        upscaleRow.AddComponent(ButtonBuilder.CreateSecondaryButton("♻️", MidjourneyStyleImageGenerationResponses.GetRedoButtonId()).Build());

        var components = new ComponentBuilder();
        components.AddRow(upscaleRow);
        components.AddRow(variantRow);
        components.AddRow(outpaintRow);
        return components;
    }
    #endregion
}

public static class MuteCommandContextImageGenerationExtensions
{
    public static async Task GenerateImage(this MuteCommandContext context, string prompt)
    {
        // Get dependencies
        var storage = context.Services.GetRequiredService<IImageGenerationConfigStorage>();
        var blacklist = context.Services.GetRequiredService<IImageGeneratorBannedWords>();
        var generator = context.Services.GetRequiredService<IImageGenerator>();
        var upscaler = context.Services.GetRequiredService<IImageUpscaler>();
        var outpainter = context.Services.GetRequiredService<IImageOutpainter>();
        var http = context.Services.GetRequiredService<HttpClient>();
        var muteConfig = context.Services.GetRequiredService<Configuration>();

        // Parse the prompt
        var parsedPrompt = Parse(prompt, context.IsPrivate, blacklist);

        // Find any images in the reference
        var images = context.Message.GetMessageImageAttachments();

        // Chose a reference image to do img2img for
        var referenceUrl = images.Shuffle().FirstOrDefault()?.Url;

        // Send a reply message
        var reply = await context.Channel.SendMessageAsync(
            "Starting image generation...",
            allowedMentions: AllowedMentions.All,
            messageReference: new MessageReference(context.Message.Id)
        );

        // Save the config into the DB
        var config = new ImageGenerationConfig
        {
            Positive = parsedPrompt.Positive,
            Negative = parsedPrompt.Negative,
            EyePositive = parsedPrompt.EyeEnhancementPositive,
            EyeNegative = parsedPrompt.EyeEnhancementNegative,
            HandPositive = parsedPrompt.HandEnhancementPositive,
            HandNegative = parsedPrompt.HandEnhancementNegative,
            FacePositive = parsedPrompt.FaceEnhancementPositive,
            FaceNegative = parsedPrompt.FaceEnhancementNegative,

            ReferenceImageUrl = referenceUrl,
            IsPrivate = context.IsPrivate,
            Type = ImageGenerationType.Generate,
            BatchSize = muteConfig.ImageGeneration?.BatchSize ?? 2,
        };
        await storage.Put(reply.Id, config);

        // Do the actual work
        var ctx = new MuteCommandContextGenerationContext(config, generator, upscaler, outpainter, http, reply);
        await ctx.Run();
    }

    #region prompt filtering
    private static readonly IReadOnlyList<string> BaseNegative = new[] { "easynegative, badhandv4, bad-hands-5, logo, Watermark, username, signature, jpeg artifacts" };

    private static Prompt Parse(string input, bool isPrivate, IImageGeneratorBannedWords blacklist)
    {
        var result = new Prompt
        {
            Positive = "",
            Negative = "",
        };

        var lines = input.Split('\n').ToList();
        if (lines.Count == 0)
            return result;

        foreach (var (line, index) in lines.Select((a, i) => (a, i)))
        {
            var split = line
                .Replace(" not ", " not ", StringComparison.OrdinalIgnoreCase)
                .Split(" not ", StringSplitOptions.RemoveEmptyEntries);

            var positive = split[0];
            var negative = string.Join(", ", split.Skip(1));
            (positive, negative) = PreprocessPrompt(positive, negative, isPrivate, blacklist);

            if (index == 0)
            {
                result.Positive = positive;
                result.Negative = negative;
            }
            else
            {
                if (line.StartsWith("hands: "))
                {
                    result.HandEnhancementPositive = positive[7..];
                    result.HandEnhancementNegative = negative;
                }
                else if (line.StartsWith("face: "))
                {
                    result.FaceEnhancementPositive = positive[6..];
                    result.FaceEnhancementNegative = negative;
                }
                else if (line.StartsWith("eyes: "))
                {
                    result.EyeEnhancementPositive = positive[6..];
                    result.EyeEnhancementNegative = negative;
                }
            }
        }

        return result;
    }

    private static (string, string) PreprocessPrompt(string positive, string negative, bool isPrivate, IImageGeneratorBannedWords blacklist)
    {
        // Add in all the help negatives
        var negativeBuilder = new StringBuilder();
        foreach (var item in BaseNegative)
        {
            if (!negative.Contains(item, StringComparison.OrdinalIgnoreCase))
            {
                negativeBuilder.Append(item);
                negativeBuilder.Append(',');
            }
        }
        negativeBuilder.Append(negative);

        // If it's a public channel apply extra precautions
        if (!isPrivate)
        {
            if (blacklist.IsBanned(positive))
                throw new ImageGenerationPrivateChannelRequiredException();

            negativeBuilder.Append(", (nsfw:1.4), (spider:1.4)");
        }

        return (positive, negativeBuilder.ToString());
    }
    #endregion
}