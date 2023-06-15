using System;
using Autofocus;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using MoreLinq;
using static Mute.Moe.Services.ImageGen.IImageGenerator;

namespace Mute.Moe.Services.ImageGen
{
    public class Automatic1111
        : IImageGenerator
    {
        private readonly string[]? _urls;

        public Automatic1111(Configuration config)
        {
            _urls = config.Automatic1111?.Urls;
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

        public async Task<Stream> GenerateImage(int seed, string positive, string negative, Func<ProgressReport, Task>? progressReporter = null)
        {
            var backend = await GetBackend();
            if (backend == null)
                throw new InvalidOperationException("No image generation backends accessible");

            var model = await backend.StableDiffusionModel("cardosAnime_v20");
            var sampler = await backend.Sampler("DPM++ SDE");

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
                    var progress = await backend.Progress();
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
