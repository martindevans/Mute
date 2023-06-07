using System;
using Autofocus;
using System.IO;
using System.Threading.Tasks;
using static Mute.Moe.Services.ImageGen.IImageGenerator;

namespace Mute.Moe.Services.ImageGen
{
    public class Automatic1111
        : IImageGenerator
    {
        private readonly StableDiffusion _api;

        public Automatic1111(Configuration config)
        {
            _api = new StableDiffusion(config.Automatic1111?.Url);
        }

        public async Task<Stream> GenerateImage(int seed, string positive, string negative, Func<ProgressReport, Task>? progressReporter = null)
        {
            var model = await _api.StableDiffusionModel("cardosAnime_v20");
            var sampler = await _api.Sampler("DPM++ SDE");

            var resultTask = _api.TextToImage(
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
                        SamplingSteps = 20,
                    },

                    Model = model,
                    BatchSize = 1,
                    Batches = 1,
                    Width = 512,
                    Height = 768,
                }
            );

            if (progressReporter != null)
            {
                while (!resultTask.IsCompleted)
                {
                    var progress = await _api.Progress();
                    await progressReporter(new ProgressReport(
                        (float)progress.Progress,
                        progress.CurrentImage == null ? null : new MemoryStream(progress.CurrentImage.Data.ToArray())
                    ));

                    await Task.Delay(250);
                }
            }

            var result = await resultTask;
            return new MemoryStream(result.Images[0].Data.ToArray());
        }
    }
}
