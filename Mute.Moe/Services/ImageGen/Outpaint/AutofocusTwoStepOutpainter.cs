using System.Threading.Tasks;
using Autofocus.Models;
using Autofocus;
using Autofocus.Config;
using Autofocus.ImageSharp.Extensions;
using Autofocus.Outpaint;
using SixLabors.ImageSharp;

namespace Mute.Moe.Services.ImageGen.Outpaint;

public class AutofocusTwoStepOutpainter
    : IImageOutpainter
{
    private readonly TwoStepOutpainter _outpainter;

    public AutofocusTwoStepOutpainter(IStableDiffusion api, IStableDiffusionModel model, ISampler sampler, int batchSize1, int batchSize2, int steps)
    {
        _outpainter = new TwoStepOutpainter(api, model, sampler)
        {
            BatchSize1 = batchSize1,
            BatchSize2 = batchSize2,
            Steps = steps
        };
    }

    public async Task<IReadOnlyCollection<Image>> Outpaint(Image image, string positive, string negative, Func<ProgressReport, Task>? progressReporter = null)
    {
        progressReporter ??= _ => Task.CompletedTask;

        var base64 = await _outpainter.Outpaint(
            new PromptConfig
            {
                Positive = positive,
                Negative = negative
            },
            image,
            progressReporter
        );

        var results = new List<Image>();
        foreach (var item in base64)
            results.Add(await item.ToImageSharpAsync());
        return results;

    }
}