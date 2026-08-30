using Discord.WebSocket;
using Mute.Moe.Services.Host;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Mute.Moe.Services.Archive;

/// <summary>
/// Add messages to archive as they arrive
/// </summary>
public sealed class ArchiveLiveFill
    : IHostedService
{
    private readonly DiscordSocketClient _discord;
    private readonly IChatArchive _archive;
    private readonly ILogger<ArchiveLiveFill> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArchiveLiveFill"/> class.
    /// </summary>
    /// <param name="discord">The Discord socket client used to receive messages and events.</param>
    /// <param name="archive">The chat archive where messages will be stored.</param>
    /// <param name="logger"></param>
    public ArchiveLiveFill(DiscordSocketClient discord, IChatArchive archive, ILogger<ArchiveLiveFill> logger)
    {
        _discord = discord;
        _archive = archive;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _discord.MessageReceived += OnMessageReceived;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _discord.MessageReceived -= OnMessageReceived;
        
        return Task.CompletedTask;
    }

    private async Task OnMessageReceived(SocketMessage socketMessage)
    {
        try
        {
            await _archive.Insert(socketMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to archive message {messageId}", socketMessage.Id);
        }
    }
}