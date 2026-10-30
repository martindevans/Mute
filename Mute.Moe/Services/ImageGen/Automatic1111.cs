using Autofocus;
using Autofocus.Config;
using Autofocus.Utilities.Progress;
using System.Threading.Tasks;
using Autofocus.SkiaSharp.Extensions;
using SkiaSharp;

namespace Mute.Moe.Services.ImageGen;

/// <summary>
/// Implementation of various image related services, using Automatic1111 API
/// </summary>
public class Automatic1111
    : IImageGenerator
{
    private readonly StableDiffusionBackendCache _backends;

    private readonly string _checkpoint;

    private readonly SamplerOptions _t2iSampler;
    private readonly string[] _t2iLoras;
    private readonly SamplerOptions _i2iSampler;
    private readonly string[] _i2iLoras;

    private readonly uint _width;
    private readonly uint _height;

    private readonly uint _img2imgClipSkip;
    private readonly uint _txt2imgClipSkip;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="config"></param>
    /// <param name="backends"></param>
    public Automatic1111(Configuration config, StableDiffusionBackendCache backends)
    {
        _backends = backends;

        _checkpoint = config.Automatic1111?.Checkpoint ?? "cardosAnime_v20";

        _t2iSampler = new SamplerOptions(
            config.Automatic1111?.Text2ImageSampler ?? "UniPC",
            config.Automatic1111?.Text2ImageScheduler ?? "karras",
            config.Automatic1111?.Text2ImageGuidanceScale ?? 5,
            config.Automatic1111?.SamplerSteps ?? 18
        );
        _t2iLoras = config.Automatic1111?.Image2ImageLoras ?? [ ];

        _i2iSampler = new SamplerOptions(
            config.Automatic1111?.Image2ImageSampler ?? "UniPC",
            config.Automatic1111?.Image2ImageScheduler ?? "karras",
            config.Automatic1111?.Image2ImageGuidanceScale ?? 5,
            config.Automatic1111?.SamplerSteps ?? 18
        );
        _i2iLoras = config.Automatic1111?.Image2ImageLoras ?? [ ];

        _width = config.Automatic1111?.Width ?? 512;
        _height = config.Automatic1111?.Height ?? 768;

        _img2imgClipSkip = config.Automatic1111?.Image2ImageClipSkip ?? 2;
        _txt2imgClipSkip = config.Automatic1111?.Text2ImageClipSkip ?? 2;
    }

    private async Task<StableDiffusionBackendCache.IBackendAccessor> GetBackend()
    {
        return await _backends.GetBackend()
            ?? throw new InvalidOperationException("No image generation backends accessible");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Base64EncodedImage>> Text2Image(int? seed, Prompt prompt, Func<ProgressReport, Task>? progressReporter = null, int batch = 1)
    {
        // Get the backend and lock it for the duration of this operation
        using var scope = await (await GetBackend()).Lock(default);
        var backend = scope.Backend;

        // Make some progress
        await (progressReporter?.Invoke(new ProgressReport(0.1f, null)) ?? Task.CompletedTask);

        // Generate images
        var rawResults = await backend.TextToImage(
            new()
            {
                Seed = new() { Seed = seed },

                Prompt = new()
                {
                    Positive = prompt.Positive,
                    Negative = prompt.Negative,
                },

                Sampler = await _t2iSampler.ToSamplerConfig(scope),
                Lora = await GetLorasConfig(backend, _t2iLoras),

                Model = await backend.StableDiffusionModel(_checkpoint),
                BatchSize = batch,
                Batches = 1,
                Width = _width,
                Height = _height,

                ClipSkip = _txt2imgClipSkip,
            }
        );

        return rawResults.Images;
    }

    private static async Task<LoraConfig[]> GetLorasConfig(IStableDiffusion backend, string[] loras)
    {
        var output = new List<LoraConfig>();
        foreach (var lora in loras)
            output.Add(new LoraConfig(await backend.Lora(lora)));
        return output.ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Base64EncodedImage>> Image2Image(int? seed, SKBitmap inputImage, Prompt prompt, Func<ProgressReport, Task>? progressReporter = null, int batch = 1)
    {
        // Get the backend and lock it for the duration of this operation
        using var scope = await (await GetBackend()).Lock(default);
        var backend = scope.Backend;

        var model = await backend.StableDiffusionModel(_checkpoint);

        // Resize image to correct size
        using var resized = inputImage.ResizeFitWithin(checked((int)_width), checked((int)_height));

        // Make some progress
        await (progressReporter?.Invoke(new ProgressReport(0.1f, null)) ?? Task.CompletedTask);

        var result = await backend.Image2Image(
            new()
            {
                Images =
                {
                    await resized.ToAutofocusImageAsync()
                },

                Seed = new() { Seed = seed },

                Prompt = new()
                {
                    Positive = prompt.Positive,
                    Negative = prompt.Negative,
                },

                Sampler = await _i2iSampler.ToSamplerConfig(scope),
                DenoisingStrength = 0.75,
                Lora = await GetLorasConfig(backend, _i2iLoras),

                Model = model,
                BatchSize = batch,
                Batches = 1,
                Width = (uint)resized.Width,
                Height = (uint)resized.Height,

                ClipSkip = _img2imgClipSkip,
            }
        );

        return result.Images;
    }

    private record SamplerOptions(string Sampler, string Scheduler, float CFG, int Steps)
    {
        public async Task<SamplerConfig> ToSamplerConfig(StableDiffusionBackendCache.BackendScope scope)
        {
            return new SamplerConfig
            {
                Sampler = await scope.Backend.Sampler(Sampler),
                Scheduler = await scope.Backend.Scheduler(Scheduler),
                SamplingSteps = scope.Steps(Steps),
                CfgScale = CFG,
            };
        }
    }
}