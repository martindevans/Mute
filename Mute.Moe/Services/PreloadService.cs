using System;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Mute.Moe.Discord.Services.Avatar;
using Mute.Moe.Discord.Services.Games;
using Mute.Moe.Services.Introspection.Uptime;
using Mute.Moe.Services.Notifications.RSS;
using Mute.Moe.Services.Notifications.SpaceX;
using Mute.Moe.Services.Reminders;
using Mute.Moe.Services.Sentiment;
using Mute.Moe.Services.Sentiment.Training;
using Mute.Moe.Services.SolariumGame;

namespace Mute.Moe.Services
{
    public class ServicePreloader
        : IHostedService
    {
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;

        private readonly Type[] _types = {
            typeof(GameService),
            typeof(AutoReactionTrainer),
            typeof(ISolarium),
            typeof(IReminderSender),
            typeof(ISentimentEvaluator),
            typeof(IUptime),
            typeof(ISpacexNotificationsSender),
            typeof(IRssNotificationsSender),
            typeof(SeasonalAvatar)
        };

        public ServicePreloader(DiscordSocketClient client, IServiceProvider services)
        {
            _client = client;
            _services = services;
        }

        [NotNull] public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Get information about a guild, when this completes it means the bot is in a sensible state to start service preloading
            await _client.Rest.GetGuildAsync(415655090842763265);

            // Wait a short extra time, just to be sure
            await Task.Delay(1000, cancellationToken);

            foreach (var type in _types)
            {
                Console.WriteLine($"Preloading Service: {type.Name}");
                _services.GetService(type);
            }
        }

        [NotNull] public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
