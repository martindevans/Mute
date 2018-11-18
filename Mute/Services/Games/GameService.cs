using System.Data.SQLite;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using JetBrains.Annotations;

namespace Mute.Services.Games
{
    public class GameService
    {
        private const string InsertGamePlayed = "INSERT OR IGNORE INTO `Games_Played` (`UserId`,`GameId`) VALUES (@UserId,@GameId);";

        [NotNull] private readonly DiscordSocketClient _client;
        [NotNull] private readonly IDatabaseService _database;

        public GameService([NotNull] DiscordSocketClient client, [NotNull] IDatabaseService database)
        {
            _client = client;
            _database = database;

            client.GuildMemberUpdated += Updated;

            database.Exec("CREATE TABLE IF NOT EXISTS `Games_Played` (`UserId` TEXT NOT NULL, `GameId` TEXT NOT NULL, PRIMARY KEY(`UserId`,`GameId`));");
            database.Exec("CREATE INDEX IF NOT EXISTS `GamesPlayedByUser` ON `Games_Played` (`UserId` ASC);");
            database.Exec("CREATE INDEX IF NOT EXISTS `GamesPlayedByGame` ON `Games_Played` (`GameId` ASC);");
        }

        private async Task Updated([NotNull] SocketUser _, [NotNull] SocketUser b)
        {
            if (b.Activity.Type != ActivityType.Playing)
                return;

            if (string.IsNullOrWhiteSpace(b.Activity.Name))
                return;

            using (var cmd = _database.CreateCommand())
            {
                cmd.CommandText = InsertGamePlayed;
                cmd.Parameters.Add(new SQLiteParameter("@UserId", System.Data.DbType.String) {Value = b.Id});
                cmd.Parameters.Add(new SQLiteParameter("@GameId", System.Data.DbType.String) {Value = b.Activity.Name});

                var count = await cmd.ExecuteNonQueryAsync();

                if (count > 0)
                {
                    //Get `unlimited-bot-works` channel
                    var c = _client.GetGuild(415655090842763265).GetChannel(445018769622958091);
                    await ((ISocketMessageChannel)c).SendMessageAsync($"{b.Username} is playing a new game: `{b.Activity.Name}`");
                }
            }
        }
    }
}
