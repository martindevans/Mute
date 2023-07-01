using System.Net.Http;
using System.Threading;
using Discord.WebSocket;
using System.Threading.Tasks;
using Discord;
using Mute.Moe.Discord.Context;
using Mute.Moe.Extensions;
using Mute.Moe.Services.Host;
using Mute.Moe.Services.ImageGen;
using Mute.Moe.Utilities;
using Image = SixLabors.ImageSharp.Image;

namespace Mute.Moe.Discord.Services.ImageGeneration
{
    public class MidjourneyStyleImageGenerationResponses
        : IHostedService
    {
        private const string MidjourneyStylePrefix = "MJButton";
        private const string VariantButtonId = MidjourneyStylePrefix + "VariantButtonId_";
        private const string UpscaleButtonId = MidjourneyStylePrefix + "UpscaleButtonId_";
        private const string RedoButtonId = MidjourneyStylePrefix + "RedoButtonId";

        private const string RedoRecursionMarker = "RedoRecursion";

        private readonly IImageGenerator _generator;
        private readonly IImageUpscaler _upscaler;
        private readonly HttpClient _http;
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;
        private readonly int _batchSize;

        private readonly AsyncLock _lock = new();

        public MidjourneyStyleImageGenerationResponses(IImageGenerator generator, IImageUpscaler upscaler, HttpClient http, DiscordSocketClient client, IServiceProvider services)
        {
            _generator = generator;
            _upscaler = upscaler;
            _http = http;
            _client = client;
            _services = services;

            _batchSize = 2;

            _client.ButtonExecuted += args =>
            {
                if (!args.Data.CustomId.StartsWith(MidjourneyStylePrefix))
                    return Task.CompletedTask;

                Task.Run(async () =>
                {
                    try
                    {
                        await OnExecuted(args);
                    }
                    catch (Exception ex)
                    {
                        await args.Channel.SendMessageAsync(ex.Message);
                    }
                });

                return Task.CompletedTask;
            };
        }

        private async Task OnExecuted(SocketMessageComponent args)
        {
            // Tell discord that we're working on it. Without this Discord times out within 3 seconds.
            await args.DeferLoadingAsync(true);

            // Take the lock, only one generation at a time
            using var locked = await _lock.LockAsync();

            // Get necessary stuff to do work
            using var attachment = await GetAttachmentImage(args);
            if (attachment == null)
                return;
            var prompt = await GetPrompt(attachment, args);
            if (prompt == null)
                return;

            // Do the work
            if (args.Data.CustomId.StartsWith(RedoButtonId))
                await Regenerate(prompt.Value, args);
            else if (args.Data.CustomId.StartsWith(VariantButtonId))
                await GenerateVariant(attachment, prompt.Value, args);
            else if (args.Data.CustomId.StartsWith(UpscaleButtonId))
                await GenerateUpscale(attachment, args);
        }
        
        private async Task Regenerate((string, string) prompt, SocketMessageComponent args)
        {
            var ctx = new MuteCommandContext(_client, args.Message, _services);
            await ctx.GenerateImage(prompt.Item1, prompt.Item2, async (_, _, r) => await _generator.Text2Image(null, prompt.Item1, prompt.Item2, r, _batchSize), true);

            //todo: the original image may have been image2image, take that into account when regenerating
            //todo: walk back up the string of reference images, looking at the button IDs to see if they were redos as well
        }

        private async Task GenerateVariant(Image original, (string, string) prompt, SocketMessageComponent args)
        {
            var ctx = new MuteCommandContext(_client, args.Message, _services);
            await ctx.GenerateImage(prompt.Item1, prompt.Item2, async (_, _, r) => await _generator.Image2Image(null, original, prompt.Item1, prompt.Item2, r, _batchSize), false);
        }

        private async Task GenerateUpscale(Image original, SocketMessageComponent args)
        {
            var ctx = new MuteCommandContext(_client, args.Message, _services);
            await ctx.GenerateImage("", "", async (_, _, r) =>
            {
                var img = await _upscaler.UpscaleImage(original, (uint)original.Width * 2, (uint)original.Height * 2, r);
                return new[] { img };
            },
            false);
        }

        #region helpers
        private static int ParseIndex(string customId)
        {
            if (customId == RedoButtonId)
                return 0;

            var span = customId.AsSpan();

            var count = 0;
            for (var i = span.Length - 1; i >= 0; i--)
            {
                if (char.IsDigit(span[i]))
                    count++;
                else
                    break;
            }

            if (!int.TryParse(span[^count..], out var number))
                return 0;
            return number;
        }

        private async Task<Image?> GetAttachmentImage(SocketMessageComponent args)
        {
            var index = ParseIndex(args.Data.CustomId);

            if (args.Message.Attachments.Count == 0)
            {
                await args.FollowupAsync("There don't seem to be any attachments on that message");
                return null;
            }

            if (index >= args.Message.Attachments.Count)
            {
                await args.FollowupAsync("There don't seem to be enough attachments on that message");
                return null;
            }

            var attachment = args.Message.Attachments.Skip(index).First();
            if (!attachment.ContentType.StartsWith("image/"))
            {
                await args.FollowupAsync("That attachment doesn't seem to be an image");
                return null;
            }

            return await Image.LoadAsync(await _http.GetStreamAsync(attachment.Url));
        }

        private async Task<(string, string)?> GetPrompt(Image attachment, SocketMessageComponent args)
        {
            var prompt = attachment.GetGenerationPrompt();
            if (prompt == null)
            {
                await args.FollowupAsync("I can't extract generation parameters from that image");
                return null;
            }

            return prompt;
        }
        #endregion

        #region send message with images
        public static ComponentBuilder CreateButtons(int count, bool isRedo)
        {
            var upscaleRow = new ActionRowBuilder();
            var variantRow = new ActionRowBuilder();
            for (var i = 0; i < count; i++)
            {
                upscaleRow.AddComponent(ButtonBuilder.CreatePrimaryButton($"U{i + 1}", GetUpscaleButtonId(i)).Build());
                variantRow.AddComponent(ButtonBuilder.CreateSuccessButton($"V{i + 1}", GetVariantButtonId(i)).Build());
            }

            upscaleRow.AddComponent(ButtonBuilder.CreateSecondaryButton("♻️", GetRedoButtonId(isRedo)).Build());

            var components = new ComponentBuilder();
            components.AddRow(upscaleRow);
            components.AddRow(variantRow);

            return components;
        }
        #endregion

        #region static button IDs
        private static string GetVariantButtonId(int index)
        {
            return VariantButtonId + index;
        }

        private static string GetUpscaleButtonId(int index)
        {
            return UpscaleButtonId + index;
        }

        private static string GetRedoButtonId(bool isRedo)
        {
            return RedoButtonId + (isRedo ? RedoRecursionMarker : "");
        }
        #endregion

        #region hosted service stuff
        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
        #endregion
    }
}
