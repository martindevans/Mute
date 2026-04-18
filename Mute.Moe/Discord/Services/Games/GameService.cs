using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Discord;
using Discord.WebSocket;
using Mute.Moe.Services.Database;
using Mute.Moe.Services.Host;

namespace Mute.Moe.Discord.Services.Games;

/// <summary>
/// Monitor user "playing" status, store them in the DB
/// </summary>
[UsedImplicitly]
public class GameService
    : IHostedService
{
    private const string InsertGamePlayed = "INSERT OR IGNORE INTO `Games_Played` (`UserId`,`GameId`) VALUES (@UserId,@GameId);";

    private readonly DiscordSocketClient _client;
    private readonly IDatabaseService _database;

    /// <summary>
    /// Create a new <see cref="GameService"/>
    /// </summary>
    /// <param name="client"></param>
    /// <param name="database"></param>
    public GameService(DiscordSocketClient client, IDatabaseService database)
    {
        _client = client;
        _database = database;

        using (var connection = database.GetConnection())
        {
            connection.Execute("CREATE TABLE IF NOT EXISTS `Games_Played` (`UserId` TEXT NOT NULL, `GameId` TEXT NOT NULL, PRIMARY KEY(`UserId`,`GameId`));");
            connection.Execute("CREATE INDEX IF NOT EXISTS `GamesPlayedByUser` ON `Games_Played` (`UserId` ASC);");
            connection.Execute("CREATE INDEX IF NOT EXISTS `GamesPlayedByGame` ON `Games_Played` (`GameId` ASC);");
        }
    }

    private async Task Updated(Cacheable<SocketGuildUser, ulong> _, SocketGuildUser user)
    {
        using var connection = _database.GetConnection();
        
        foreach (var activity in user.Activities)
        {
            if (activity?.Type != ActivityType.Playing)
                continue;

            if (string.IsNullOrWhiteSpace(activity.Name))
                continue;

            var count = await connection.ExecuteAsync(
                InsertGamePlayed,
                new
                {
                    UserId = user.Id.ToString(),
                    GameId = activity.Name
                }
            );

            if (count > 0)
            {
                // Get `unlimited-bot-works` channel
                var c = _client.GetGuild(415655090842763265).GetChannel(445018769622958091);
                await ((ISocketMessageChannel)c).SendMessageAsync($"{user.Username} is playing a new game: `{activity.Name}`");
            }
        }
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _client.GuildMemberUpdated += Updated;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _client.GuildMemberUpdated -= Updated;
    }
}