using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Mute.Moe.Discord.Services;
using Mute.Moe.Discord.Services.Games;
using Mute.Moe.Services.Introspection.Uptime;
using Mute.Moe.Services.Reminders;
using Mute.Moe.Services.Sentiment;

namespace Mute.Moe.Services
{
    public class ServicePreloader
        : IHostedService
    {
        public ServicePreloader(GameService games, HistoryLoggingService history, ReactionSentimentTrainer reactionTrainer, IReminderSender reminders, ISentimentEvaluator sentiment, IUptime uptime)
        {
            // Parameters to this services cause those other services to be eagerly initialised.
        }

        [NotNull] public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        [NotNull] public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
