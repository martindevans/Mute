﻿using Discord;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Mute.Moe.Discord.Context;

namespace Mute.Moe.Services.ImageGen;

public interface IImageGenerator
{
    Task<Stream> GenerateImage(int seed, string positive, string negative, Func<ProgressReport, Task>? progress = null);

    public record struct ProgressReport(float Progress, MemoryStream? Intermediate);
}

public static class IImageGeneratorExtensions
{
    public static async Task EasyGenerate(this IImageGenerator generator, MuteCommandContext context, string prompt)
    {
        var negative = "easynegative, badhandv4, logo, Watermark, username, signature, jpeg artifacts";

        // If it's a public channel apply extra precautions
        if (!context.IsPrivate)
        {
            var ban = context.Services.GetRequiredService<IImageGeneratorBannedWords>();
            if (ban.IsBanned(prompt))
            {
                await context.Channel.SendMessageAsync("Sorry, I can't generate that image (use a DM channel to disable filters).");
                return;
            }

            negative += ", (nsfw:1.5)";
        }

        var reply = await context.Channel.SendMessageAsync(
            "Starting image generation...",
            allowedMentions: AllowedMentions.All,
            messageReference: new MessageReference(context.Message.Id)
        );

        var rng = context.Services.GetRequiredService<Random>();
        try
        {
            // Do the generation
            var reporter = new ImageProgressReporter(reply);
            var result = await generator.GenerateImage(rng.Next(), prompt, negative, reporter.ReportAsync);

            // Send a final result
            await reporter.FinalImage(result);
        }
        catch (Exception ex)
        {
            await reply.ModifyAsync(msg => msg.Content = $"Image generation failed!\n{ex.Message}");
        }
    }

    private class ImageProgressReporter
    {
        private readonly IUserMessage _message;

        public ImageProgressReporter(IUserMessage message)
        {
            _message = message;
        }

        public async Task ReportAsync(IImageGenerator.ProgressReport value)
        {
            //if (value.Intermediate != null)
            //{
            //    await _message.ModifyAsync(props =>
            //    {
            //        props.Attachments = new Optional<IEnumerable<FileAttachment>>(new[]
            //        {
            //            new FileAttachment(value.Intermediate, "WIP.png")
            //        });
            //    });

            //}
            //else
            {
                await _message.ModifyAsync(props =>
                {
                    props.Content = $"Generating ({value.Progress:P0})";
                });
            }
        }

        public async Task FinalImage(Stream image)
        {
            await _message.ModifyAsync(props =>
            {
                props.Attachments = new Optional<IEnumerable<FileAttachment>>(new[]
                {
                    new FileAttachment(image, "diffusion.png")
                });

                props.Content = "";
            });
        }
    }
}