﻿using System.Threading.Tasks;
using Autofocus.Outpaint;
using SixLabors.ImageSharp;

namespace Mute.Moe.Services.ImageGen;

public interface IImageOutpainter
{
    public Task<IReadOnlyCollection<Image>> Outpaint(Image image, string positive, string negative, Func<ProgressReport, Task>? progressReporter = null);
}