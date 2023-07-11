using Autofocus;
using System.IO;
using System.Threading.Tasks;
using Autofocus.Config;
using Autofocus.CtrlNet;
using Autofocus.Extensions.AfterDetailer;
using SixLabors.ImageSharp.Processing;
using Autofocus.ImageSharp.Extensions;
using Autofocus.Scripts.UltimateUpscaler;
using Mute.Moe.Extensions;
using SixLabors.ImageSharp;
using Image = SixLabors.ImageSharp.Image;
using Mute.Moe.Services.ImageGen.Outpaint;
using SixLabors.ImageSharp.PixelFormats;

namespace Mute.Moe.Services.ImageGen;

public class Automatic1111
    : IImageGenerator, IImageAnalyser, IImageUpscaler, IImageOutpainter
{
    private readonly StableDiffusionBackendCache _backends;

    private readonly string _checkpoint;
    private readonly string _t2iSampler;
    private readonly string _i2iSampler;
    private readonly int _samplerSteps;
    private readonly uint _width;
    private readonly uint _height;
    private readonly string _upscaler;

    private readonly uint _img2imgClipSkip;
    private readonly uint _txt2imgClipSkip;

    public Automatic1111(Configuration config, StableDiffusionBackendCache backends)
    {
        _backends = backends;

        _checkpoint = config.Automatic1111?.Checkpoint ?? "cardosAnime_v20";
        _t2iSampler = config.Automatic1111?.Text2ImageSampler ?? "UniPC";
        _i2iSampler = config.Automatic1111?.Image2ImageSampler ?? "DDIM";
        _samplerSteps = config.Automatic1111?.SamplerSteps ?? 18;
        _width = config.Automatic1111?.Width ?? 512;
        _height = config.Automatic1111?.Height ?? 768;
        _upscaler = config.Automatic1111?.Upscaler ?? "Lanczos";

        _img2imgClipSkip = config.Automatic1111?.Image2ImageClipSkip ?? 2;
        _txt2imgClipSkip = config.Automatic1111?.Text2ImageClipSkip ?? 2;
    }

    private Task<StableDiffusion?> GetBackend()
    {
        return _backends.GetBackend();
    }

    public async Task<IReadOnlyCollection<Image>> Text2Image(int? seed, Prompt prompt, Func<IImageGenerator.ProgressReport, Task>? progressReporter = null, int batch = 1)
    {
        var backend = await GetBackend() ?? throw new InvalidOperationException("No image generation backends accessible");

        var model = await backend.StableDiffusionModel(_checkpoint);
        var sampler = await backend.Sampler(_t2iSampler);

        var rawResults = await PumpProgress(backend, backend.TextToImage(
            new()
            {
                Seed = new() { Seed = seed },

                Prompt = new()
                {
                    Positive = prompt.Positive,
                    Negative = prompt.Negative,
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

    public async Task<IReadOnlyCollection<Image>> Image2Image(int? seed, Image inputImage, Prompt prompt, Func<IImageGenerator.ProgressReport, Task>? progressReporter = null, int batch = 1)
    {
        var backend = await GetBackend() ?? throw new InvalidOperationException("No image generation backends accessible");

        var model = await backend.StableDiffusionModel(_checkpoint);
        var sampler = await backend.Sampler(_i2iSampler);

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
            .SelectAwait(async a => await a.ToImageSharpAsync())
            .ToListAsync();
    }

    private static AfterDetailer GetAfterDetailer(Prompt prompt)
    {
        return new AfterDetailer
        {
            Steps = {
                new()
                {
                    Model = "face_yolov8n.pt",
                    PositivePrompt = prompt.FaceEnhancementPositive,
                    NegativePrompt = prompt.FaceEnhancementNegative,
                },
                new()
                {
                    Model = "mediapipe_face_mesh_eyes_only",
                    PositivePrompt = prompt.EyeEnhancementPositive,
                    NegativePrompt = prompt.EyeEnhancementNegative,
                },
                new()
                {
                    Model = "hand_yolov8n.pt",
                    PositivePrompt = prompt.HandEnhancementPositive,
                    NegativePrompt = prompt.HandEnhancementNegative,
                }
            }
        };
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

    public async Task<Image> UpscaleImage(Image inputImage, uint width, uint height, Func<IImageGenerator.ProgressReport, Task>? progressReporter = null)
    {
        var backend = await GetBackend() ?? throw new InvalidOperationException("No image analysis backends accessible");

        var model = await backend.StableDiffusionModel(_checkpoint);
        var sampler = await backend.Sampler(_i2iSampler);
        var upscaler = await backend.Upscaler(_upscaler);

        // Try to get the generation prompt that was originally used for this image
        var prompt = inputImage.GetGenerationPrompt() ?? (
            "detailed, <lora:add_detail:0.5>",
            "easynegative, nsfw, badhandv4"
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
        ), progressReporter);

        return await upscaleResult.Images[0].ToImageSharpAsync();
    }

    public async Task<IReadOnlyCollection<Image>> Outpaint(Image inputImage, string positive, string negative, Func<IImageGenerator.ProgressReport, Task>? progressReporter = null)
    {
        var backend = await GetBackend() ?? throw new InvalidOperationException("No image analysis backends accessible");
        var model = await backend.StableDiffusionModel(_checkpoint);
        var sampler = await backend.Sampler(_i2iSampler);
        var outpainter = new TwoStepOutpainter(backend, model, sampler, 2, 2, 75);

        // Clone input image before mutation
        using var image = inputImage.CloneAs<Rgba32>();

        // Scale down to the correct width
        image.Mutate(a => a.Resize(new Size((int)_width, 0)));

        // Scale down height if necessary
        if (image.Height > _height)
            image.Mutate(a => a.Resize(new Size(0, (int)_height)));

        // Do the actual outpainting
        var result = await outpainter.Outpaint(new PromptConfig { Positive = positive, Negative = negative, }, image, Progess);

        // Decode all images
        var results = new List<Image>();
        foreach (var item in result)
            results.Add(await item.ToImageSharpAsync());
        return results;

        void Progess(float p)
        {
            progressReporter?.Invoke(new IImageGenerator.ProgressReport(p, null));
        }
    }

    private static async Task<T> PumpProgress<T>(IStableDiffusion backend, Task<T> task, Func<IImageGenerator.ProgressReport, Task>? progressReporter)
    {
        while (!task.IsCompleted)
        {
            try
            {
                if (progressReporter != null)
                {
                    var progress = await backend.Progress(true);
                    await progressReporter(new IImageGenerator.ProgressReport((float)progress.Progress, null));
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
}