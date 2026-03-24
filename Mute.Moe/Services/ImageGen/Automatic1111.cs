using Autofocus;
using Autofocus.Config;
using Autofocus.ImageSharp.Extensions;
using Autofocus.Scripts.UltimateUpscaler;
using Autofocus.Utilities.Progress;
using Mute.Moe.Services.ImageGen.Outpaint;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Autofocus.FeatureRepaint;
using Image = SixLabors.ImageSharp.Image;

namespace Mute.Moe.Services.ImageGen;

/// <summary>
/// Implementation of various image related services, using Automatic1111 API
/// </summary>
public class Automatic1111
    : IImageGenerator, IImageAnalyser, IImageUpscaler, IImageOutpainter
{
    private readonly StableDiffusionBackendCache _backends;

    private readonly string _checkpoint;

    private readonly SamplerOptions _t2iSampler;
    private readonly string[] _t2iLoras;
    private readonly SamplerOptions _i2iSampler;
    private readonly string[] _i2iLoras;

    private readonly int _outpaintSteps;
    private readonly uint _width;
    private readonly uint _height;
    private readonly string _upscaler;

    private readonly uint _img2imgClipSkip;
    private readonly uint _txt2imgClipSkip;

    private const InterrogateModel _model = InterrogateModel.DeepDanbooru;
    string IImageAnalyser.ModelName => _model.ToString();

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

        _outpaintSteps = config.Automatic1111?.OutpaintSteps ?? 75;
        _width = config.Automatic1111?.Width ?? 512;
        _height = config.Automatic1111?.Height ?? 768;
        _upscaler = config.Automatic1111?.Upscaler ?? "Lanczos";

        _img2imgClipSkip = config.Automatic1111?.Image2ImageClipSkip ?? 2;
        _txt2imgClipSkip = config.Automatic1111?.Text2ImageClipSkip ?? 2;
    }

    private async Task<StableDiffusionBackendCache.IBackendAccessor> GetBackend()
    {
        return await _backends.GetBackend()
            ?? throw new InvalidOperationException("No image generation backends accessible");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Image>> Text2Image(int? seed, Prompt prompt, Func<ProgressReport, Task>? progressReporter = null, int batch = 1)
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

        return await ExtractImagesWithRepaint(rawResults.Images, scope, prompt, progressReporter);
    }

    private static async Task<LoraConfig[]> GetLorasConfig(IStableDiffusion backend, string[] loras)
    {
        var output = new List<LoraConfig>();
        foreach (var lora in loras)
            output.Add(new LoraConfig(await backend.Lora(lora)));
        return output.ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Image>> Image2Image(int? seed, Image inputImage, Prompt prompt, Func<ProgressReport, Task>? progressReporter = null, int batch = 1)
    {
        // Get the backend and lock it for the duration of this operation
        using var scope = await (await GetBackend()).Lock(default);
        var backend = scope.Backend;

        var model = await backend.StableDiffusionModel(_checkpoint);

        // Clone input image before mutation
        using var image = inputImage.CloneAs<Rgba32>();

        // Scale down to the correct width
        image.Mutate(a => a.Resize(new Size((int)_width, 0)));

        // Scale down height if necessary
        if (image.Height > _height)
            image.Mutate(a => a.Resize(new Size(0, (int)_height)));

        var autofocusInputImage = await image.ToAutofocusImageAsync();

        // Make some progress
        await (progressReporter?.Invoke(new ProgressReport(0.1f, null)) ?? Task.CompletedTask);

        var result = await backend.Image2Image(
            new()
            {
                Images =
                {
                    autofocusInputImage
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
                Width = (uint)image.Width,
                Height = (uint)image.Height,

                ClipSkip = _img2imgClipSkip,
            }
        );

        return await ExtractImagesWithRepaint(result.Images, scope, prompt, progressReporter);
    }

    /// <inheritdoc />
    public async Task<ImageAnalysisResult?> GetImageDescription(Stream image, CancellationToken cancellation = default)
    {
        // Get the backend and lock it for the duration of this operation
        using var scope = await (await GetBackend()).Lock(default);
        var backend = scope.Backend;

        var mem = new MemoryStream();
        await image.CopyToAsync(mem, cancellation);
        var buffer = mem.ToArray();

        var analysis = await backend.Interrogate(new Base64EncodedImage(buffer), _model, cancellation);

        var desc = analysis.Caption.Replace("\\(", "(").Replace("\\)", ")");

        return new ImageAnalysisResult(null, desc);
    }

    /// <inheritdoc />
    public async Task<Image> UpscaleImage(Image inputImage, uint width, uint height, Func<ProgressReport, Task>? progressReporter = null)
    {
        // Get the backend and lock it for the duration of this operation
        using var scope = await (await GetBackend()).Lock(default);
        var backend = scope.Backend;

        var model = await backend.StableDiffusionModel(_checkpoint);
        var upscaler = await backend.Upscaler(_upscaler);

        // Try to get the generation prompt that was originally used for this image
        var prompt = inputImage.GetGenerationPrompt() ?? (
            "detailed",
            "nsfw, bad quality"
        );

        var upscaleResult = await backend.Image2Image(
            new()
            {
                Images =
                {
                    await inputImage.ToAutofocusImageAsync(),
                },

                Model = model,

                Prompt = new()
                {
                    Positive = prompt.Item1,
                    Negative = prompt.Item2,
                },

                Seed = new(),

                Sampler = await _i2iSampler.ToSamplerConfig(scope),
                Lora = await GetLorasConfig(backend, _i2iLoras),

                Width = width,
                Height = height,
                DenoisingStrength = 0.25,
                Script = new UltimateUpscale
                {
                    RedrawMode = RedrawMode.Chess,
                    Upscaler = upscaler
                }
            }
        );

        return await upscaleResult.Images[0].ToImageSharpAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Image>> Outpaint(Image inputImage, string positive, string negative, Func<ProgressReport, Task>? progressReporter = null)
    {
        // Get the backend and lock it for the duration of this operation
        using var scope = await (await GetBackend()).Lock(default);
        var backend = scope.Backend;

        var model = await backend.StableDiffusionModel(_checkpoint);
        var conf = await _i2iSampler.ToSamplerConfig(scope);
        var outpainter = new AutofocusTwoStepOutpainter(backend, model, conf.Sampler, conf.Scheduler, 2, 1, scope.Steps(_outpaintSteps));

        // Clone input image before mutation
        using var image = inputImage.CloneAs<Rgba32>();

        // Scale down to the correct width
        image.Mutate(a => a.Resize(new Size((int)_width, 0)));

        // Scale down height if necessary
        if (image.Height > _height)
            image.Mutate(a => a.Resize(new Size(0, (int)_height)));

        // Do the actual outpainting
        return await outpainter.Outpaint(image, positive, negative, progressReporter);
    }

    private async Task<List<Image>> ExtractImagesWithRepaint(
        IReadOnlyList<Base64EncodedImage> rawResults,
        StableDiffusionBackendCache.BackendScope scope,
        Prompt prompt,
        Func<ProgressReport, Task>? progressReporter
    )
    {
        // Should we redraw faces?
        var redraw = !string.IsNullOrWhiteSpace(prompt.FaceEnhancementPositive) || !string.IsNullOrWhiteSpace(prompt.EyeEnhancementPositive);
        FeatureRepainter? repainter = null;
        if (redraw)
        {
            repainter = new FeatureRepainter(
                scope.Backend,
                await scope.Backend.StableDiffusionModel(_checkpoint),
                await _i2iSampler.ToSamplerConfig(scope),
                await GetLorasConfig(scope.Backend, _t2iLoras)
            );
        }

        // Update progress report
        if (progressReporter != null)
            await progressReporter(new ProgressReport(redraw ? 0.5f : 0.9f, null));

        // Extract results and apply repainting
        var progressStep = 0.45f / rawResults.Count;
        var progress = 0.5f;
        var results = new List<Image>();
        foreach (var item in rawResults)
        {
            var itemImage = await item.ToImageSharpAsync<Rgb24>();
            if (redraw)
            {
                using (itemImage)
                    results.Add(await RepaintFaces(itemImage, repainter!, prompt));

                progress += progressStep;
                await (progressReporter?.Invoke(new ProgressReport(progress, null)) ?? Task.CompletedTask);
            }
            else
            {
                results.Add(itemImage);
            }
        }

        return results;
    }

    private static async Task<Image> RepaintFaces(Image<Rgb24> input, FeatureRepainter repainter, Prompt prompt)
    {
        var positivesF = prompt.FaceEnhancementPositive?.Split("[SEP]") ?? [];
        var negativesF = prompt.FaceEnhancementNegative?.Split("[SEP]") ?? [];
        var positivesE = prompt.EyeEnhancementPositive?.Split("[SEP]") ?? [];
        var negativesE = prompt.EyeEnhancementNegative?.Split("[SEP]") ?? [];

        var count = Math.Max(Math.Max(positivesF.Length, positivesE.Length), Math.Max(negativesF.Length, negativesE.Length));
        var prompts = new List<FacePrompt>();
        for (var i = 0; i < count; i++)
        {
            var pf = GetIndexOrLast(positivesF, i);
            var nf = GetIndexOrLast(negativesF, i);
            var pe = GetIndexOrLast(positivesE, i);
            var ne = GetIndexOrLast(negativesE, i);
            var n = $"{nf}, {ne}";
            prompts.Add(new FacePrompt(pf, pe, n));
        }

        var analysis = await repainter.Analyse(input, new AnalysisConfig()
        {
            MinSize = (64, 64),
            MaxDetections = 3,
            MinConfidence = 0.5f,
        });

        var result = await repainter.Repaint(
            input,
            analysis,
            prompts
        );

        return result;

        static string GetIndexOrLast(string[]? items, int index)
        {
            if (items == null || items.Length == 0)
                return "";

            return index >= items.Length ? items[^1] : items[index];
        }
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