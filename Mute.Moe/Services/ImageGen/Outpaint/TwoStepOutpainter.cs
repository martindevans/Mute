using System.Threading.Tasks;
using Autofocus;
using Autofocus.Config;
using Autofocus.ImageSharp.Extensions;
using Autofocus.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Mute.Moe.Services.ImageGen.Outpaint
{
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

            // Create an image expanded outwards by 128 in all directions
            using var inputImage = new Image<Rgba32>(input.Width + 256, input.Height + 256);
            inputImage.Mutate(ctx =>
            {
                var gfxOptions = new GraphicsOptions
                {
                    Antialias = true,
                    ColorBlendingMode = PixelColorBlendingMode.Normal,
                };

                ctx.Fill(average);
                ctx.DrawImage(input, new Point(128, 128), gfxOptions);
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

                    Sampler = new()
                    {
                        Sampler = _sampler,
                        SamplingSteps = _steps1,
                        CfgScale = 4
                    },
                })
            );

            var progPerBatch = 0.5f / result1.Images.Count;
            var baseProgress = 0.5f;
            var results = new List<Base64EncodedImage>();
            foreach (var image in result1.Images)
            {
                progress(baseProgress);
                {
                    var inner = await Redraw(image, (uint)inputMask.Width, (uint)inputMask.Height, prompt, -1);
                    results.AddRange(inner);
                }
                baseProgress += progPerBatch;
                progress(baseProgress);
            }
            return results;
        }

        private async Task<IReadOnlyCollection<Base64EncodedImage>> Redraw(Base64EncodedImage input, uint width, uint height, PromptConfig prompt, SeedConfig seed)
        {
            var result2 = await _api.Image2Image(new ImageToImageConfig
            {
                Images =
                {
                    input,
                },

                Model = _model,
                DenoisingStrength = 0.25,

                BatchSize = _batchSize2,

                Width = width,
                Height = height,

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
                var value = await _api.Progress(true);
                progress(min + (max - min) * (float)value.Progress);
                await Task.Delay(350);
            }

            return await task;
        }
    }
}
