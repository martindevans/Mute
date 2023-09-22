using System.Net.Http;
using System.Text;
using Discord.Interactions;
using JetBrains.Annotations;
using Mute.Moe.Services.ImageGen;
using System.Threading.Tasks;
using Autofocus.Config;
using Discord;
using Mute.Moe.Extensions;
using SixLabors.ImageSharp;

namespace Mute.Moe.Discord.Interactions
{
    [Group("image", "Image generation")]
    [UsedImplicitly]
    public class Pictures
        : InteractionModuleBase
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

        [SlashCommand("status", "I will check the status of the image generation backends")]
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

        [SlashCommand("analyse", "I will try to describe the image")]
        public async Task Analyse(IAttachment attachment, InterrogateModel model = InterrogateModel.DeepDanbooru)
        {
            await DeferAsync();

            var image = await attachment.GetPngStream(_http);
            if (image == null)
            {
                await FollowupAsync("Please include an image attachment!");
                return;
            }

            var description = await _analyser.GetImageDescription(image, model);

            var embed = new EmbedBuilder()
                       .WithImageUrl(attachment.Url)
                       .Build();
            await FollowupAsync($"```{description}```", embed: embed);
        }

        [SlashCommand("metadata", "I will try to extract stable diffusion generation data from the image")]
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
    }
}
