using Autofocus;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Autofocus.Config;
using Autofocus.CtrlNet;
using Autofocus.Extensions.AfterDetailer;
using SixLabors.ImageSharp.Processing;
using Autofocus.ImageSharp.Extensions;
using Autofocus.Scripts.UltimateUpscaler;
using Autofocus.Utilities.Progress;
using Mute.Moe.Services.ImageGen.Outpaint;
using SixLabors.ImageSharp;
using Image = SixLabors.ImageSharp.Image;
using SixLabors.ImageSharp.PixelFormats;

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

    private readonly float _afterDetailHandMinSize;
    private readonly float _afterDetailFaceMinSize;
    private readonly bool _enableAfterDetailer;

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

        _enableAfterDetailer = config.Automatic1111?.AfterDetail?.Enable ?? false;
        _afterDetailHandMinSize = config.Automatic1111?.AfterDetail?.HandMinSize ?? 0.05f;
        _afterDetailFaceMinSize = config.Automatic1111?.AfterDetail?.FaceMinSize ?? 0.05f;
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

        var model = await backend.StableDiffusionModel(_checkpoint);

        var rawResults = await PumpProgress(backend, backend.TextToImage(
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

                Model = model,
                BatchSize = batch,
                Batches = 1,
                Width = _width,
                Height = _height,

                ClipSkip = _txt2imgClipSkip,

                AdditionalScripts = {
                    GetAfterDetailer(prompt)
                },
    }
        ), progressReporter);

        var results = new List<Image>();
        foreach (var item in rawResults.Images)
            results.Add(await item.ToImageSharpAsync());
        return results;
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

            var cnetModel = await cnet.Model("control_v11p_sd15s2_lineart_anime [3825e83e]");
            if (cnetModel != null)
            {
                cnetConfig = new ControlNetConfig
                {
                    Image = preprocessed.Images[0],
                    Model = cnetModel,
                    GuidanceStart = 0.1,
                    GuidanceEnd = 0.5,
                    ControlMode = ControlMode.PromptImportant,
                    Weight = 0.25
                };
            }
        }

        var result = await PumpProgress(backend, backend.Image2Image(
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

                AdditionalScripts =
                {
                    cnetConfig,
                    GetAfterDetailer(prompt)
                }
            }
        ), progressReporter);
        
        return await result.Images
            .Take(result.Images.Count - (cnetConfig == null ? 0 : 1)) // Skip the last image if cnet is used, to remove the guidance image which is added to the end
            .ToAsyncEnumerable()
            .Select(async (Base64EncodedImage a, CancellationToken _) => await a.ToImageSharpAsync())
            .ToListAsync();
    }

    private AfterDetailer? GetAfterDetailer(Prompt prompt)
    {
        if (!_enableAfterDetailer)
            return null;

        return new AfterDetailer
        {
            Steps = {
                new()
                {
                    Model = "face_yolov8n.pt",
                    PositivePrompt = Join(prompt.FaceEnhancementPositive, prompt.EyeEnhancementPositive),
                    NegativePrompt = Join(prompt.FaceEnhancementNegative, prompt.EyeEnhancementNegative),

                    SamplerSteps = _i2iSampler.Steps,
                    MaskMinRatio = _afterDetailFaceMinSize,
                },
                //new()
                //{
                //    Model = "mediapipe_face_mesh_eyes_only",
                //    PositivePrompt = prompt.EyeEnhancementPositive,
                //    NegativePrompt = prompt.EyeEnhancementNegative,
                //},
                new()
                {
                    Model = "hand_yolov8n.pt",
                    PositivePrompt = prompt.HandEnhancementPositive,
                    NegativePrompt = prompt.HandEnhancementNegative,

                    SamplerSteps = _i2iSampler.Steps,
                    InpaintSize = (448, 448),
                    MaskMinRatio = _afterDetailHandMinSize,
                }
            }
        };

        static string? Join(string? left, string? right)
        {
            return (string.IsNullOrWhiteSpace(left), string.IsNullOrWhiteSpace(right)) switch
            {
                (true, true) => null,
                (true, false) => right,
                (false, true) => left,
                (false, false) => $"{left}, {right}",
            };
        }
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

        var upscaleResult = await PumpProgress(backend, backend.Image2Image(
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
        ), progressReporter);

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

    private static async Task<T> PumpProgress<T>(IStableDiffusion backend, Task<T> task, Func<ProgressReport, Task>? progressReporter)
    {
        while (!task.IsCompleted)
        {
            try
            {
                if (progressReporter != null)
                {
                    var progress = await backend.Progress(true);
                    await progressReporter(new ProgressReport((float)progress.Progress, null));
                }
            }
            catch (TimeoutException)
            {

            }
            finally
            {
                await Task.Delay(350);
            }
        }

        return await task;
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