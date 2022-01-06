using System.Data.SQLite;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Mute.Moe.Services.Database;
using Mute.Moe.Services.Host;

namespace Mute.Moe.Discord.Services.Games
{
    public class GameService
        : IHostedService
    {
        private const string InsertGamePlayed = "INSERT OR IGNORE INTO `Games_Played` (`UserId`,`GameId`) VALUES (@UserId,@GameId);";

         private readonly DiscordSocketClient _client;
         private readonly IDatabaseService _database;

        public GameService( DiscordSocketClient client,  IDatabaseService database)
        {
            _client = client;
            _database = database;
            
            database.Exec("CREATE TABLE IF NOT EXISTS `Games_Played` (`UserId` TEXT NOT NULL, `GameId` TEXT NOT NULL, PRIMARY KEY(`UserId`,`GameId`));");
            database.Exec("CREATE INDEX IF NOT EXISTS `GamesPlayedByUser` ON `Games_Played` (`UserId` ASC);");
            database.Exec("CREATE INDEX IF NOT EXISTS `GamesPlayedByGame` ON `Games_Played` (`GameId` ASC);");
        }

        private async Task Updated(Cacheable<SocketGuildUser, ulong> _, SocketGuildUser user)
        {
            foreach (var activity in user.Activities)
            {
                if (activity?.Type != ActivityType.Playing)
                    continue;

                if (string.IsNullOrWhiteSpace(activity.Name))
                    continue;

                await using var cmd = _database.CreateCommand();
                cmd.CommandText = InsertGamePlayed;
                cmd.Parameters.Add(new SQLiteParameter("@UserId", System.Data.DbType.String) { Value = user.Id });
                cmd.Parameters.Add(new SQLiteParameter("@GameId", System.Data.DbType.String) { Value = activity.Name });

                var count = await cmd.ExecuteNonQueryAsync();

                if (count > 0)
                {
                    //Get `unlimited-bot-works` channel
                    var c = _client.GetGuild(415655090842763265).GetChannel(445018769622958091);
                    await ((ISocketMessageChannel)c).SendMessageAsync($"{user.Username} is playing a new game: `{activity.Name}`");
                }
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _client.GuildMemberUpdated += Updated;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _client.GuildMemberUpdated -= Updated;
        }
    }
}
