using System.Net.Http;
using System.Text;
using Discord.Interactions;
using Mute.Moe.Services.ImageGen;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Mute.Moe.Discord.Services.ImageGeneration;
using Mute.Moe.Services.ImageGen.Contexts;
using SixLabors.ImageSharp;

namespace Mute.Moe.Discord.Interactions;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// Generate images with AI
/// </summary>
[Group("image", "Image generation")]
[UsedImplicitly]
public class Pictures
    : MuteInteractionModuleBase
{
    private readonly IImageAnalyser _analyser;
    private readonly HttpClient _http;
    private readonly StableDiffusionBackendCache _backends;
    private readonly IImageGenerationConfigStorage _storage;
    private readonly IImageGenerator _generator;
    private readonly IImageUpscaler _upscaler;
    private readonly IImageOutpainter _outpainter;

    /// <summary>
    /// Construct a new <see cref="Pictures"/> module
    /// </summary>
    /// <param name="analyser"></param>
    /// <param name="http"></param>
    /// <param name="backends"></param>
    /// <param name="storage"></param>
    /// <param name="generator"></param>
    /// <param name="upscaler"></param>
    /// <param name="outpainter"></param>
    public Pictures(IImageAnalyser analyser, HttpClient http, StableDiffusionBackendCache backends, IImageGenerationConfigStorage storage, IImageGenerator generator, IImageUpscaler upscaler, IImageOutpainter outpainter)
    {
        _analyser = analyser;
        _http = http;
        _backends = backends;
        _storage = storage;
        _generator = generator;
        _upscaler = upscaler;
        _outpainter = outpainter;
    }

    /// <summary>
    /// Check if image generation backends are available
    /// </summary>
    /// <returns></returns>
    [SlashCommand("status", "I will check the status of the image generation backends")]
    [UsedImplicitly]
    public async Task Status()
    {
        await DeferAsync();

        var backends = await _backends.GetBackends(true);

        if (backends.Count == 0)
        {
            await FollowupAsync("No backends");
        }
        else
        {
            var index = 0;
            var builder = new StringBuilder();
            foreach (var (name, count, availabled) in backends)
                builder.AppendLine($"{++index}. `{name}` has been used {count} times, ({(availabled ? "Ok" : "Unresponsive")})");

            await FollowupAsync(builder.ToString());
        }
    }

    /// <summary>
    /// Analyse image, produce textual description of it
    /// </summary>
    /// <param name="attachment"></param>
    [SlashCommand("analyse", "I will try to describe the image")]
    [UsedImplicitly]
    public async Task Analyse(IAttachment attachment)
    {
        await DeferAsync();

        var image = await attachment.GetPngStream(_http);
        if (image == null)
        {
            await FollowupAsync("Please include an image attachment!");
            return;
        }

        var analysis = await _analyser.GetImageDescription(image);
        var desc = analysis?.Description ?? "Something went wrong analysing that image";
        var title = analysis?.Title ?? attachment.Title ?? "Image Analysis";

        var localTag = _analyser.IsLocal ? " (local)" : "";
        var embed = new EmbedBuilder()
                   .WithImageUrl(attachment.Url)
                   .WithFooter($"{_analyser.ModelName}{localTag}")
                   .WithDescription(desc)
                   .WithTitle(title)
                   .Build();
        await FollowupAsync(embed: embed);
    }

    [SlashCommand("metadata", "I will try to extract stable diffusion generation data from the image")]
    [UsedImplicitly]
    public async Task Metadata(IAttachment attachment)
    {
        await DeferAsync();

        var image = await attachment.GetPngStream(_http);
        if (image == null)
        {
            await FollowupAsync("Please include an image attachment!");
            return;
        }

        var img = await SixLabors.ImageSharp.Image.IdentifyAsync(image);
        var meta = img.Metadata.GetPngMetadata();
        var parameters = meta.GetGenerationMetadata();

        if (parameters != null)
        {
            var embed = new EmbedBuilder()
                       .WithImageUrl(attachment.Url)
                       .Build();
            await FollowupAsync($"```{parameters}```", embed: embed);
        }
        else
        {
            await FollowupAsync("I couldn't find any metadata in that image");
        }
    }

    [ComponentInteraction($"{MidjourneyStyleImageGenerationButtons.RedoButtonFullId}", ignoreGroupNames: true)]
    [UsedImplicitly]
    public Task MidjourneyButton()
    {
        return MidjourneyButtonWithIndex(MidjourneyStyleImageGenerationButtons.RedoButtonPrefix, "0");
    }

    [ComponentInteraction($"{MidjourneyStyleImageGenerationButtons.CommonPrefix}*_*", ignoreGroupNames:true)]
    [UsedImplicitly]
    public async Task MidjourneyButtonWithIndex(string buttonType, string buttonIndex)
    {
        try
        {
            var args = (SocketMessageComponent)Context.Interaction;
            await args.DeferLoadingAsync();

            if (!int.TryParse(buttonIndex, out var parsedButtonIndex))
                parsedButtonIndex = 0;

            // Get the config that was used to generate this.
            // If it's null it's probably because a legacy style button was pressed, try to make up a best-guess config.
            var config = await _storage.Get(args.Message.Id)
                      ?? await LegacyConfig(args);

            // Nothing needs doing for redo button, config fetched from storage is already correct
            if (buttonType != MidjourneyStyleImageGenerationButtons.RedoButtonPrefix)
            {
                // Get the attachment the button wants to work on
                config.ReferenceImageUrl = (await GetAttachment(args, parsedButtonIndex))?.Url;

                // Pick generation type
                if (buttonType.StartsWith(MidjourneyStyleImageGenerationButtons.VariantButtonPrefix))
                    config.Type = ImageGenerationType.Generate;
                else if (buttonType.StartsWith(MidjourneyStyleImageGenerationButtons.UpscaleButtonPrefix))
                    config.Type = ImageGenerationType.Upscale;
                else if (buttonType.StartsWith(MidjourneyStyleImageGenerationButtons.OutpaintButtonPrefix))
                    config.Type = ImageGenerationType.Outpaint;
                else
                    throw new InvalidOperationException($"Unknown button type: {buttonType}");
            }

            // Do the actual generation!
            await new SocketMessageComponentGenerationContext(config, _generator, _upscaler, _outpainter, _http, args, _storage).Run();
        }
        catch (Exception ex)
        {
            await ReplyAsync2($"Interaction Exception! {ex.Message}");
            throw;
        }
    }

    #region helpers
    private async Task<ImageGenerationConfig> LegacyConfig(SocketMessageComponent args)
    {
        // Lets just assume the message is the prompt
        var positive = args.Message.Content;
        var negative = "(nsfw), (spider)";

        // Try to extract the prompt from the image attachments
        var firstImage = args.Message.Attachments.FirstOrDefault(a => a.ContentType.StartsWith("image/"));
        if (firstImage != null)
        {
            var image = await SixLabors.ImageSharp.Image.LoadAsync(await _http.GetStreamAsync(firstImage.Url));
            var prompt = image.GetGenerationPrompt();
            if (prompt != null)
                (positive, negative) = prompt.Value;
        }

        return new ImageGenerationConfig
        {
            BatchSize = 2,
            IsPrivate = args.IsDMInteraction,
            Positive = positive,
            Negative = negative,
            ReferenceImageUrl = null,
            Type = ImageGenerationType.Generate
        };
    }

    private static async Task<Attachment?> GetAttachment(SocketMessageComponent args, int index)
    {
        if (args.Message.Attachments.Count == 0)
        {
            await args.FollowupAsync("There don't seem to be any attachments on that message");
            return null;
        }

        if (index >= args.Message.Attachments.Count)
        {
            await args.FollowupAsync("There don't seem to be enough attachments on that message");
            return null;
        }

        var attachment = args.Message.Attachments.Skip(index).First();
        if (!attachment.ContentType.StartsWith("image/"))
        {
            await args.FollowupAsync("That attachment doesn't seem to be an image");
            return null;
        }

        return attachment;
    }
    #endregion
}