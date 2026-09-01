using Discord;
using Discord.WebSocket;
using Mute.Moe.Services.Host;
using System.Threading;
using System.Threading.Tasks;
using Mute.Moe.Services.Notifications.Cron;
using Microsoft.Extensions.Logging;

namespace Mute.Moe.Services.Archive
{
    /// <summary>
    /// Tries to fill in holes in the message archive
    /// </summary>
    [UsedImplicitly]
    public sealed class ArchiveCatchup
        : IHostedService
    {
        private readonly DiscordSocketClient _discord;
        private readonly IChatArchive _archive;
        private readonly ICron _cron;
        private readonly IChatArchiveHoles _holes;
        private readonly ILogger<ArchiveCatchup> _logger;
        private readonly CancellationTokenSource _cts;

        /// <summary>
        /// Initializes a new instance of the <see cref="ArchiveCatchup"/> class.
        /// </summary>
        /// <param name="discord">The Discord socket client used to interact with Discord channels and events.</param>
        /// <param name="archive">The chat archive service used to store and retrieve message data.</param>
        /// <param name="cron"></param>
        /// <param name="holes"></param>
        /// <param name="logger"></param>
        public ArchiveCatchup(DiscordSocketClient discord, IChatArchive archive, ICron cron, IChatArchiveHoles holes, ILogger<ArchiveCatchup> logger)
        {
            _discord = discord;
            _archive = archive;
            _cron = cron;
            _holes = holes;
            _logger = logger;
            _cts = new CancellationTokenSource();
        }
        
        /// <inheritdoc />
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _discord.ChannelCreated += OnChannelCreated;

            // Schedule catchup for all channels
            // ReSharper disable once MethodSupportsCancellation
            await BeginMessageCatchup(cancellationToken);

            // Process holes with a randomised interval to prevent flooding the discord API
            _ = _cron.RandomInterval(
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(300),
                ProcessHole,
                _cts.Token
            );
        }

        /// <inheritdoc />
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _discord.ChannelCreated -= OnChannelCreated;

            _cts.Cancel();
            
            return Task.CompletedTask;
        }

        private async Task OnChannelCreated(SocketChannel socketChannel)
        {
            await ScheduleChannelCatchup(socketChannel as IMessageChannel);
        }

        private async Task BeginMessageCatchup(CancellationToken cancellationToken)
        {
            foreach (var guild in _discord.Guilds)
            foreach (var channel in guild.Channels)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await ScheduleChannelCatchup(channel as IMessageChannel);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to schedule catchup for channel {channel}", channel.Id);
                }

                await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
            }
        }

        private async ValueTask ScheduleChannelCatchup(IMessageChannel? channel)
        {
            if (channel == null)
                return;

            // Get the latest message sent in the channel, explore backwards from there
            var latest = (await channel.GetMessagesAsync(1).FlattenAsync()).FirstOrDefault();
            if (latest != null)
                await _holes.Create(latest.Channel.Id, latest.Id, forward: false);
        }

        /// <summary>
        /// Get a hole from the DB and process it now
        /// </summary>
        /// <returns></returns>
        private async Task ProcessHole()
        {
            // Get a hole to fill
            var hole = await _holes.Read();
            if (hole == null)
                return;

            // Get the channel
            if (_discord.GetChannel(hole.ChannelId) is IMessageChannel channel)
            {
                // Get some messages before or after
                var messages = (await channel.GetMessagesAsync(
                    hole.StartMessageId,
                    hole.Forward ? Direction.After : Direction.Before,
                    64
                ).FlattenAsync()).ToArray();

                if (messages.Length > 0)
                {
                    // Insert all these messages
                    var anyNew = false;
                    foreach (var message in messages)
                    {
                        anyNew |= await _archive.Insert(message);
                        await Task.Delay(TimeSpan.FromMilliseconds(100));
                    }

                    var continuation = hole.Forward
                        ? messages.MaxBy(a => a.CreatedAt)
                        : messages.MinBy(a => a.CreatedAt);

                    // If any were new, continue filling from the end of this block
                    if (anyNew && continuation != null)
                        await _holes.Create(hole.ChannelId, continuation.Id, hole.Forward);
                }
            }

            // We've processed this hole
            await _holes.Delete(hole.Id);
        }
    }
}
