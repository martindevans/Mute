using Autofocus;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Autofocus.Config;
using Autofocus.CtrlNet;
using MoreLinq;
using SixLabors.ImageSharp.Processing;
using Autofocus.ImageSharp.Extensions;
using SixLabors.ImageSharp;

namespace Mute.Moe.Services.ImageGen;

public class Automatic1111
    : IImageGenerator, IImageAnalyser
{
    private readonly string[]? _urls;

    private readonly string _checkpoint;
    private readonly string _t2iSampler;
    private readonly string _i2iSampler;
    private readonly int _samplerSteps;
    private readonly uint _width;
    private readonly uint _height;

    public Automatic1111(Configuration config)
    {
        _urls = config.Automatic1111?.Urls;

        _checkpoint = config.Automatic1111?.Checkpoint ?? "cardosAnime_v20";
        _t2iSampler = config.Automatic1111?.Text2ImageSampler ?? "UniPC";
        _i2iSampler = config.Automatic1111?.Image2ImageSampler ?? "DDIM";
        _samplerSteps = config.Automatic1111?.SamplerSteps ?? 12;
        _width = config.Automatic1111?.Width ?? 512;
        _height = config.Automatic1111?.Height ?? 768;
    }

    private async Task<StableDiffusion?> GetBackend()
    {
        if (_urls == null || _urls.Length == 0)
            return null;

        foreach (var url in _urls.Shuffle())
        {
            var api = new StableDiffusion(url);

            try
            {
                // Ping this backend to see if it's accessable
                await api.Progress();
                return api;
            }
            catch (HttpRequestException)
            {
                // Suppress these exceptions, they mean that the backend is inaccessible and we
                // just want to skip it.
            }
        }

        return null;
    }

    public async Task<Stream> Text2Image(int seed, string positive, string negative, Func<IImageGenerator.ProgressReport, Task>? progressReporter = null)
    {
        var backend = await GetBackend()
                   ?? throw new InvalidOperationException("No image generation backends accessible");

        var model = await backend.StableDiffusionModel(_checkpoint);
        var sampler = await backend.Sampler(_t2iSampler);

        var resultTask = backend.TextToImage(
            new()
            {
                Seed = new() { Seed = seed },

                Prompt = new()
                {
                    Positive = positive,
                    Negative = negative,
                },

                Sampler = new()
                {
                    Sampler = sampler,
                    SamplingSteps = _samplerSteps,
                },

                Model = model,
                BatchSize = 1,
                Batches = 1,
                Width = _width,
                Height = _height,
            }
        );

        if (progressReporter != null)
        {
            while (!resultTask.IsCompleted)
            {
                var progress = await backend.Progress(true);
                await progressReporter(new IImageGenerator.ProgressReport((float)progress.Progress, null));
                await Task.Delay(250);
            }
        }

        var result = await resultTask;
        return new MemoryStream(result.Images[0].Data.ToArray());
    }

    public async Task<Stream> Image2Image(int seed, Stream imageStream, string positive, string negative, Func<IImageGenerator.ProgressReport, Task>? progressReporter = null)
    {
        var backend = await GetBackend()
                   ?? throw new InvalidOperationException("No image generation backends accessible");

        var model = await backend.StableDiffusionModel(_checkpoint);
        var sampler = await backend.Sampler(_i2iSampler);

        // Load imagesharp image
        var image = await Image.LoadAsync(imageStream);

        // Scale down to the correct width
        image.Mutate(a => a.Resize(new Size((int)_width, 0)));

        // Scale down height if necessary
        if (image.Height > _height)
            image.Mutate(a => a.Resize(new Size(0, (int)_height)));

        var autofocusInputImage = await image.ToAutofocusImageAsync();

        // Add a weak controlnet constraint if available
        ControlNetConfig? cnetConfig = null;
        var cnet = await backend.TryGetControlNet();
        if (cnet != null)
        {
            var preprocessed = await cnet.Preprocess(new ControlNetPreprocessConfig
            {
                Images = { autofocusInputImage },
                Module = await cnet.Module("lineart_anime_denoise"),
            });

            cnetConfig = new ControlNetConfig
            {
                Image = preprocessed.Images[0],
                Model = await cnet.Model("control_v11p_sd15s2_lineart_anime [3825e83e]"),
                GuidanceStart = 0.1,
                GuidanceEnd = 0.5,
                ControlMode = ControlMode.PromptImportant,
                Weight = 0.45
            };
        }

        var resultTask = backend.Image2Image(
            new()
            {
                Images =
                {
                    autofocusInputImage
                },

                Seed = new() { Seed = seed },

                Prompt = new()
                {
                    Positive = positive,
                    Negative = negative,
                },

                Sampler = new()
                {
                    Sampler = sampler,
                    SamplingSteps = _samplerSteps,
                },
                DenoisingStrength = 0.75,

                Model = model,
                BatchSize = 1,
                Batches = 1,
                Width = (uint)image.Width,
                Height = (uint)image.Height,

                AdditionalScripts =
                {
                    cnetConfig
                }
            }
        );

        if (progressReporter != null)
        {
            while (!resultTask.IsCompleted)
            {
                var progress = await backend.Progress(true);
                await progressReporter(new IImageGenerator.ProgressReport((float)progress.Progress, null));
                await Task.Delay(250);
            }
        }

        var result = await resultTask;
        return new MemoryStream(result.Images[0].Data.ToArray());
    }

    public async Task<string> GetImageDescription(Stream image, InterrogateModel model)
    {
        var backend = await GetBackend()
                   ?? throw new InvalidOperationException("No image analysis backends accessible");

        var mem = new MemoryStream();
        await image.CopyToAsync(mem);
        var buffer = mem.ToArray();

        var analysis = await backend.Interrogate(new Base64EncodedImage(buffer), model);
        return analysis.Caption;
    }
}