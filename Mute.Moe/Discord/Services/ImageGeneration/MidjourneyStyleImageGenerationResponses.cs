using System.Net.Http;
using System.Threading;
using Discord.WebSocket;
using System.Threading.Tasks;
using Discord;
using Mute.Moe.Services.Host;
using Mute.Moe.Services.ImageGen;
using Mute.Moe.Utilities;
using Mute.Moe.Services.ImageGen.Contexts;
using NAudio.SoundFont;

namespace Mute.Moe.Discord.Services.ImageGeneration
{
    public class MidjourneyStyleImageGenerationResponses
        : IHostedService
    {
        private const string MidjourneyStylePrefix = "MJButton";
        private const string VariantButtonId = MidjourneyStylePrefix + "VariantButtonId_";
        private const string UpscaleButtonId = MidjourneyStylePrefix + "UpscaleButtonId_";
        private const string RedoButtonId = MidjourneyStylePrefix + "RedoButtonId";

        private readonly IImageGenerator _generator;
        private readonly IImageUpscaler _upscaler;
        private readonly HttpClient _http;
        private readonly DiscordSocketClient _client;
        private readonly IImageGenerationConfigStorage _storage;

        private readonly AsyncLock _lock = new();

        public MidjourneyStyleImageGenerationResponses(IImageGenerator generator, IImageUpscaler upscaler, HttpClient http, DiscordSocketClient client, IImageGenerationConfigStorage storage)
        {
            _generator = generator;
            _upscaler = upscaler;
            _http = http;
            _client = client;
            _storage = storage;

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
                        if (args.HasResponded)
                            await args.ModifyOriginalResponseAsync(props => props.Content = ex.Message);
                        else
                            await args.RespondAsync(ex.Message);
                    }
                });

                return Task.CompletedTask;
            };
        }

        private async Task OnExecuted(SocketMessageComponent args)
        {
            // Tell discord that we're working on it. Without this Discord times out within 3 seconds.
            await args.DeferLoadingAsync();

            // Take the lock, only one generation at a time
            using var locked = await _lock.LockAsync();

            // Get the config that was used to generate this.
            var config = await _storage.Get(args.Message.Id);

            // If it's null it's probably because a legacy style button was pressed.Try to make up a best-guess config.
            if (config == null)
                config = await LegacyConfig(args);

            var newConfigId = (await args.GetOriginalResponseAsync()).Id;

            // Parse out the redo ID
            if (args.Data.CustomId.StartsWith(RedoButtonId))
            {
                // Nothing needs doing for a redo, the config is already correct!
            }
            else
            {
                // Get the attachment the button wants to work on
                config.ReferenceImageUrl = (await GetAttachment(args))?.Url;

                // Pick generation type
                config.Type = args.Data.CustomId.StartsWith(VariantButtonId)
                    ? ImageGenerationType.Generate
                    : ImageGenerationType.Upscale;
            }

            // Save the new config
            await _storage.Put(newConfigId, config);

            // Do the actual generation!
            await new SocketMessageComponentGenerationContext(config, _generator, _upscaler, _http, args).Run();
        }

        private static async Task<ImageGenerationConfig> LegacyConfig(SocketMessageComponent args)
        {
            return new ImageGenerationConfig
            {
                BatchSize = 2,
                IsPrivate = args.IsDMInteraction,
                Positive = args.Message.Content,
                Negative = "",
                ReferenceImageUrl = null,
                Type = ImageGenerationType.Generate
            };
        }

        #region helpers
        private static ulong ParseNumberFromEnd(string customId)
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

            if (!ulong.TryParse(span[^count..], out var number))
                return 0;
            return number;
        }

        private async Task<Attachment?> GetAttachment(SocketMessageComponent args)
        {
            var index = (int)ParseNumberFromEnd(args.Data.CustomId);

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

            return attachment;
        }
        #endregion

        #region static button IDs
        public static string GetVariantButtonId(int index)
        {
            return VariantButtonId + index;
        }

        public static string GetUpscaleButtonId(int index)
        {
            return UpscaleButtonId + index;
        }

        public static string GetRedoButtonId()
        {
            return RedoButtonId;
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
