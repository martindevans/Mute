using System.Collections.Concurrent;
using Autofocus;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Autofocus.Config;
using Autofocus.CtrlNet;
using MoreLinq;
using SixLabors.ImageSharp.Processing;
using Autofocus.ImageSharp.Extensions;
using Autofocus.Scripts.UltimateUpscaler;
using Mute.Moe.Extensions;
using SixLabors.ImageSharp;
using Image = SixLabors.ImageSharp.Image;

namespace Mute.Moe.Services.ImageGen;

public class Automatic1111
    : IImageGenerator, IImageAnalyser, IImageUpscaler
{
    private readonly string[]? _urls;

    private readonly string _checkpoint;
    private readonly string _t2iSampler;
    private readonly string _i2iSampler;
    private readonly int _samplerSteps;
    private readonly uint _width;
    private readonly uint _height;
    private readonly string _upscaler;

    public Automatic1111(Configuration config)
    {
        _urls = config.Automatic1111?.Urls;

        _checkpoint = config.Automatic1111?.Checkpoint ?? "cardosAnime_v20";
        _t2iSampler = config.Automatic1111?.Text2ImageSampler ?? "UniPC";
        _i2iSampler = config.Automatic1111?.Image2ImageSampler ?? "DDIM";
        _samplerSteps = config.Automatic1111?.SamplerSteps ?? 18;
        _width = config.Automatic1111?.Width ?? 512;
        _height = config.Automatic1111?.Height ?? 768;
        _upscaler = config.Automatic1111?.Upscaler ?? "Lanczos";
    }

    private async Task<StableDiffusion?> GetBackend()
    {
        if (_urls == null || _urls.Length == 0)
            return null;

        // Ping all endpoints in parallel to find responsive endpoints
        var successful = new ConcurrentBag<StableDiffusion>();
        await Parallel.ForEachAsync(_urls, async (url, _) =>
        {
            try
            {
                var api = new StableDiffusion(url);

                // Ping this backend to see if it's accessable
                await api.Progress();

                successful.Add(api);
            }
            catch (HttpRequestException)
            {
                // Suppress these exceptions, they mean that the backend is inaccessible and we
                // just want to skip it.
            }
        });

        // Pick a random endpoint
        return successful.Shuffle().FirstOrDefault();
    }

    public async Task<IReadOnlyCollection<Image>> Text2Image(int? seed, string positive, string negative, Func<IImageGenerator.ProgressReport, Task>? progressReporter = null, int batch = 1)
    {
        var backend = await GetBackend() ?? throw new InvalidOperationException("No image generation backends accessible");

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
                BatchSize = batch,
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

        var results = new List<Image>();
        foreach (var item in (await resultTask).Images)
            results.Add(await item.ToImageSharpAsync());
        return results;
    }

    public async Task<IReadOnlyCollection<Image>> Image2Image(int? seed, Image image, string positive, string negative, Func<IImageGenerator.ProgressReport, Task>? progressReporter = null, int batch = 1)
    {
        var backend = await GetBackend() ?? throw new InvalidOperationException("No image generation backends accessible");

        var model = await backend.StableDiffusionModel(_checkpoint);
        var sampler = await backend.Sampler(_i2iSampler);

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
                Weight = 0.25
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
                BatchSize = batch,
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
        return await result.Images
            .Take(result.Images.Count - 1)
            .ToAsyncEnumerable()
            .SelectAwait(async a => await a.ToImageSharpAsync())
            .ToListAsync();
    }

    public async Task<string> GetImageDescription(Stream image, InterrogateModel model)
    {
        var backend = await GetBackend() ?? throw new InvalidOperationException("No image analysis backends accessible");

        var mem = new MemoryStream();
        await image.CopyToAsync(mem);
        var buffer = mem.ToArray();

        var analysis = await backend.Interrogate(new Base64EncodedImage(buffer), model);
        return analysis.Caption;
    }

    public async Task<Image?> UpscaleImage(Image inputImage, uint width, uint height, Func<IImageGenerator.ProgressReport, Task>? progressReporter = null)
    {
        var backend = await GetBackend() ?? throw new InvalidOperationException("No image analysis backends accessible");

        var model = await backend.StableDiffusionModel(_checkpoint);
        var sampler = await backend.Sampler(_i2iSampler);
        var upscaler = await backend.Upscaler(_upscaler);

        // Try to get the generation prompt that was originally used for this image
        var prompt = inputImage.GetGenerationPrompt();
        if (prompt == null)
        {
            prompt = (
                "detailed, <lora:add_detail:0.5>",
                "easynegative, nsfw, badhandv4"
            );
        }

        var upscaleTask = backend.Image2Image(
            new()
            {
                Images = {
                    await inputImage.ToAutofocusImageAsync(),
                },

                Model = model,

                Prompt = new()
                {
                    Positive = prompt.Value.Item1,
                    Negative = prompt.Value.Item2,
                },

                Seed = new(),

                Sampler = new()
                {
                    Sampler = sampler,
                    SamplingSteps = _samplerSteps,
                },

                Width = width,
                Height = height,
                DenoisingStrength = 0.22,
                Script = new UltimateUpscale
                {
                    RedrawMode = RedrawMode.Chess,
                    Upscaler = upscaler
                }
            }
        );

        if (progressReporter != null)
        {
            while (!upscaleTask.IsCompleted)
            {
                var progress = await backend.Progress(true);
                await progressReporter(new IImageGenerator.ProgressReport((float)progress.Progress, null));
                await Task.Delay(250);
            }
        }

        foreach (var item in (await upscaleTask).Images)
            return await item.ToImageSharpAsync();
        return null;
    }
}