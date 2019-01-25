using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Mute.Moe.Discord.Services;
using Mute.Moe.Discord.Services.Games;
using Mute.Moe.Services.Introspection.Uptime;
using Mute.Moe.Services.Sentiment;

namespace Mute.Moe.Services
{
    public class ServicePreloader
        : IHostedService
    {
        public ServicePreloader(GameService games, HistoryLoggingService history, ReactionSentimentTrainer reactionTrainer, ReminderService reminders, ISentimentService sentiment, IUptime uptime)
        {
            // Parameters to this services cause those other services to be eagerly initialised.
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
