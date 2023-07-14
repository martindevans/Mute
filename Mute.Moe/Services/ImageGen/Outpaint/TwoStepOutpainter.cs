using System.Threading.Tasks;
using Autofocus;
using Autofocus.Config;
using Autofocus.ImageSharp.Extensions;
using Autofocus.Models;
using Mute.Moe.Extensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Mute.Moe.Services.ImageGen.Outpaint;

internal class TwoStepOutpainter
{
    private readonly StableDiffusion _api;
    private readonly IStableDiffusionModel _model;
    private readonly ISampler _sampler;

    private readonly int _batchSize1;
    private readonly int _batchSize2;
    private readonly int _steps1;
    private readonly int _steps2;

    public TwoStepOutpainter(StableDiffusion api, IStableDiffusionModel model, ISampler sampler, int batchSize1, int batchSize2, int steps)
    {
        _api = api;
        _model = model;
        _sampler = sampler;

        _batchSize1 = batchSize1;
        _steps1 = (int)Math.Ceiling(steps * 0.7);

        _batchSize2 = batchSize2;
        _steps2 = (int)Math.Ceiling(steps * 0.3);
    }

    public async Task<IReadOnlyCollection<Base64EncodedImage>> Outpaint(PromptConfig prompt, Image originalInput, Action<float> progress)
    {
        return await ExpandOutwards(prompt, originalInput, progress);
    }

    private async Task<IReadOnlyCollection<Base64EncodedImage>> ExpandOutwards(PromptConfig prompt, Image input, Action<float> progress)
    {
        // Calculate average colour of the whole image
        var average = input.AverageColor();
        progress(0.01f);

        var drawingOptions = new GraphicsOptions
        {
            Antialias = true,
            ColorBlendingMode = PixelColorBlendingMode.Normal,
        };

        // Create an image expanded outwards by 128 in all directions
        using var inputImage = new Image<Rgba32>(input.Width + 256, input.Height + 256);
        inputImage.Mutate(ctx =>
        {
            ctx.Fill(average);
            ctx.DrawImage(input, new Point(128, 128), drawingOptions);
        });
        inputImage.Bleed(new Rectangle(128, 128, input.Width - 1, input.Height - 1), 128, null, 1);
        progress(0.05f);

        // Create a mask covering the noise with a smooth transition into the image
        using var inputMask = new Image<Rgba32>(inputImage.Width, inputImage.Height);
        inputMask.Mutate(ctx =>
        {
            const int blur = 4;
            var rect = new RectangleF(128 + blur, 128 + blur, input.Width - blur * 2, input.Width - blur * 2);

            ctx.Fill(Color.White)
               .Fill(Color.Black, rect)
               .BoxBlur(blur)
               .Fill(Color.Black, rect);
        });
        progress(0.1f);

        // Shrink inputs down to the size of the original input for the first step
        inputImage.Mutate(ctx => ctx.Resize(input.Size));
        inputMask.Mutate(ctx => ctx.Resize(input.Size));

        // Run img2img over the entire image. The mask protects most of the original content from being changed at all.
        var result1 = await Pump(0.1f, 0.5f, progress,
            _api.Image2Image(new ImageToImageConfig
            {
                Images =
                {
                    await inputImage.ToAutofocusImageAsync()
                },

                Mask = await inputMask.ToAutofocusImageAsync(),

                Model = _model,
                DenoisingStrength = 0.75,

                BatchSize = _batchSize1,

                Width = (uint)inputMask.Width,
                Height = (uint)inputMask.Height,

                Prompt = prompt,

                Seed = -1,

                ClipSkip = 2,

                Sampler = new()
                {
                    Sampler = _sampler,
                    SamplingSteps = _steps1,
                    CfgScale = 4
                },
            })
        );
        progress(0.5f);
            
        var progPerBatch = 0.5f / result1.Images.Count;
        var baseProgress = 0.5f;
        var results = new List<Base64EncodedImage>();
        foreach (var item in result1.Images)
        {
            progress(baseProgress);
            {
                // Expand image back up to proper size and draw the most of the original input into the middle. This fixes the loss from when we scaled down previously.
                using var image = await item.ToImageSharpAsync();
                image.Mutate(ctx =>
                {
                    ctx.Resize(input.Size + new Size(256, 256));

                    const int margin = 8;
                    ctx.DrawImage(input, new Point(128 + margin, 128 + margin), new Rectangle(margin, margin, input.Width - margin * 2, input.Height - margin * 2), drawingOptions);
                });

                // Redraw the entire image at the full scale. This step has a very low number of steps and low denoising, so it shouldn't change the composition
                // of the overall image too much. But it should fix up the seams that we just made much worse by painting the original image back in!
                var inner = await Redraw(image, prompt, -1);
                results.AddRange(inner);
            }
            baseProgress += progPerBatch;
            progress(baseProgress);
        }
        return results;
    }

    private async Task<IReadOnlyCollection<Base64EncodedImage>> Redraw(Image input, PromptConfig prompt, SeedConfig seed)
    {
        var result2 = await _api.Image2Image(new ImageToImageConfig
        {
            Images =
            { 
                await input.ToAutofocusImageAsync(),
            },

            Model = _model,
            DenoisingStrength = 0.25,

            BatchSize = _batchSize2,

            Width = (uint)input.Width,
            Height = (uint)input.Height,

            Prompt = prompt,
            Seed = seed,

            Sampler = new()
            {
                Sampler = _sampler,
                SamplingSteps = _steps2,
                CfgScale = 8
            },
        });

        return result2.Images;
    }

    private async Task<T> Pump<T>(float min, float max, Action<float> progress, Task<T> task)
    {
        while (!task.IsCompleted)
        {
            try
            {
                var value = await _api.Progress(true);
                progress(min + (max - min) * (float)value.Progress);
            }
            catch (TimeoutException)
            {

            }
            finally
            {
                await Task.Delay(350);
            }
        }

        progress(max);
        return await task;
    }
}