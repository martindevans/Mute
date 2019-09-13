using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Grpc.Core;
using JetBrains.Annotations;
using Mute.Moe.Extensions;
using Mute.Moe.Services.Database;
using Mute.Moe.Services.SolariumGame.Modes;
using Solarium;

namespace Mute.Moe.Services.SolariumGame
{
    public class DatabaseSolarium
        : ISolarium
    {
        [NotNull] private readonly IDatabaseService _database;
        [NotNull] private readonly DiscordSocketClient _client;
        [NotNull] private readonly Configuration _config;

        private const string GetGamesSql = "SELECT * FROM SolariumGames WHERE (GuildId = @GuildId or @GuildId IS null) AND (ChannelId = @ChannelId or @ChannelId IS null);";
        private const string InsertGameSql = "INSERT INTO SolariumGames (ChannelId, GuildId, GameId, Name, Description, Mode) values(@ChannelId, @GuildId, @GameId, @Name, @Description, @Mode);";
        private const string DeleteGameSql = "DELETE FROM SolariumGames WHERE GameId = @GameId";

        private const string InsertPlayersSql = "INSERT INTO SolariumPlayers (DiscordPlayerId, GameId, SecretKey, SolariumPlayerId) values(@DiscordPlayerId, @GameId, @SecretKey, @SolariumPlayerId);";
        private const string GetPlayerSqlByDiscordId = "SELECT * FROM SolariumPlayers WHERE GameId = @GameId and DiscordPlayerId = @DiscordPlayerId";
        private const string GetPlayersSql = "SELECT * FROM SolariumPlayers WHERE GameId = @GameId";

        private const string GetCategorySql = "SELECT * FROM SolariumActivations WHERE GuildId = @GuildId;";
        private const string InsertCategorySql = "REPLACE INTO SolariumActivations (GuildId, CategoryId) values(@GuildId, @CategoryId);";

        private readonly ConcurrentDictionary<string, BaseGameModeEventHandler> _handlers = new ConcurrentDictionary<string, BaseGameModeEventHandler>();

        public DatabaseSolarium([NotNull] IDatabaseService database, [NotNull] DiscordSocketClient client, [NotNull] Configuration config)
        {
            _database = database;
            _client = client;
            _config = config;

            try
            {
                _database.Exec("CREATE TABLE IF NOT EXISTS `SolariumGames` (`ChannelId` TEXT NOT NULL,`GuildId` TEXT NOT NULL,`GameId` TEXT NOT NULL UNIQUE,`Name` TEXT NOT NULL,`Description` TEXT NOT NULL,`Mode` TEXT NOT NULL,PRIMARY KEY(`GuildId`,`ChannelId`));");
                _database.Exec("CREATE TABLE IF NOT EXISTS `SolariumActivations` (`GuildId` TEXT NOT NULL UNIQUE,`CategoryId` TEXT NOT NULL UNIQUE,PRIMARY KEY(`GuildId`));");
                _database.Exec("CREATE TABLE IF NOT EXISTS `SolariumPlayers` (`DiscordPlayerId` TEXT NOT NULL,`SolariumPlayerId` TEXT NOT NULL,`GameId` TEXT NOT NULL,`SecretKey` TEXT NOT NULL UNIQUE,PRIMARY KEY(`DiscordPlayerId`,`GameId`));");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Task.Run(SubscribeGameUpdateStreams);
        }

        private async Task SubscribeGameUpdateStreams()
        {
            _client.Ready += async () => {

                var allGames = await GetGames();

                await allGames.ForEachAsync(g => {
                    _ = Task.Run(async () => await SubscribeToGameUpdateStream(g));
                    Task.Delay(100);
                });
            };
        }

        [NotNull] private async Task SubscribeToGameUpdateStream([NotNull] IGame game, bool first = false)
        {
            var g = _client.GetGuild(game.GuildId);
            var c = g?.GetChannel(game.ChannelId);
            if (!(c is IMessageChannel text))
                return;

            // Construct game specific handler
            BaseGameModeEventHandler handler;
            switch (game.Mode)
            {
                case NewGameRequest.Types.GameMode.Thewolfgame:
                    handler = new WolfGame(game, _client, this, text);
                    break;

                default:
                    await text.SendMessageAsync($"Unknown Solarium game mode: `{game.Mode}`");
                    return;
            }

            // Save in (in memory) game collection
            var id = unchecked((uint)game.GameId.GetHashCode()).MeaninglessString();
            _handlers[id] = handler;

            // if this is the first time, print the game ID
            if (first)
            {
                var m = await text.SendMessageAsync($"`Use {_config.PrefixCharacter}solarium action {id} \"action text\"` to send private actions to this game");
                await m.PinAsync();
            }

            // Subscribe to remote events
            await handler.Start(_config.Solarium.SolariumHostAddress, ChannelCredentials.Insecure);
        }

        public Task<ulong?> GetCategory(ulong guild)
        {
            ulong ParseCategory(DbDataReader reader)
            {
                return ulong.Parse((string)reader["CategoryId"]);
            }

            DbCommand PrepareQuery(IDatabaseService db)
            {
                var cmd = db.CreateCommand();
                cmd.CommandText = GetCategorySql;
                cmd.Parameters.Add(new SQLiteParameter("@GuildId", System.Data.DbType.String) { Value = guild.ToString() });
                return cmd;
            }

            return new SqlAsyncResult<ulong>(_database, PrepareQuery, ParseCategory).Select(a => (ulong?)a).SingleOrDefault();
        }

        public async Task SetCategory(ulong guild, ulong categoryId)
        {
            try
            {
                using (var cmd = _database.CreateCommand())
                {
                    cmd.CommandText = InsertCategorySql;
                    cmd.Parameters.Add(new SQLiteParameter("@CategoryId", System.Data.DbType.String) {Value = categoryId.ToString()});
                    cmd.Parameters.Add(new SQLiteParameter("@GuildId", System.Data.DbType.String) {Value = guild.ToString()});

                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task<IAsyncEnumerable<IPlayer>> Players(IGame game)
        {
            DbCommand PrepareQuery(IDatabaseService db)
            {
                var cmd = db.CreateCommand();
                cmd.CommandText = GetPlayersSql;
                cmd.Parameters.Add(new SQLiteParameter("@GameId", System.Data.DbType.String) { Value = game.GameId });
                return cmd;
            }

            return new SqlAsyncResult<IPlayer>(_database, PrepareQuery, ParsePlayer);
        }

        public async Task<IGame> Join(ulong guildId, ulong channelId, ulong userId)
        {
            // Sanity check user exists
            var user = _client.GetUser(userId);
            if (user == null)
                return null;

            // Try to find a game in this channel
            var game = await (await GetGames(guildId, channelId)).SingleOrDefault();
            if (game == null)
                return null;

            // Join game at remote end
            var grpcChannel = new Channel(_config.Solarium.SolariumHostAddress, ChannelCredentials.Insecure);
            var client = new Solarium.Solarium.SolariumClient(grpcChannel);
            var response = await client.JoinGameAsync(new JoinGameRequest {
                GameID = game.GameId,
                Name = (user as IGuildUser)?.Nickname ?? user.Username
            });

            // Save private key for this user in this game
            try
            {
                using (var cmd = _database.CreateCommand())
                {
                    cmd.CommandText = InsertPlayersSql;
                    cmd.Parameters.Add(new SQLiteParameter("@GameId", System.Data.DbType.String) { Value = game.GameId });
                    cmd.Parameters.Add(new SQLiteParameter("@DiscordPlayerId", System.Data.DbType.String) { Value = userId.ToString() });
                    cmd.Parameters.Add(new SQLiteParameter("@SecretKey", System.Data.DbType.String) { Value = response.Secret });
                    cmd.Parameters.Add(new SQLiteParameter("@SolariumPlayerId", System.Data.DbType.String) { Value = response.ID });

                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return game;
        }

        public async Task<DoActionResponse> DoAction(Action<DoActionRequest> action, ulong user, IGame game)
        {
            DbCommand PrepareQuery(IDatabaseService db)
            {
                var cmd = db.CreateCommand();
                cmd.CommandText = GetPlayerSqlByDiscordId;
                cmd.Parameters.Add(new SQLiteParameter("@GameId", System.Data.DbType.String) { Value = game.GameId });
                cmd.Parameters.Add(new SQLiteParameter("@DiscordPlayerId", System.Data.DbType.String) { Value = user.ToString() });
                return cmd;
            }

            try
            {
                var player = await new SqlAsyncResult<IPlayer>(_database, PrepareQuery, ParsePlayer).SingleOrDefault();

                if (player == null)
                    return null;

                var act = new DoActionRequest {
                    GameID = game.GameId,
                    PlayerID = player.SolariumId,
                    PlayerSecret = player.SecretKey
                };
                action(act);

                var grpcChannel = new Channel(_config.Solarium.SolariumHostAddress, ChannelCredentials.Insecure);
                var client = new Solarium.Solarium.SolariumClient(grpcChannel);
                return await client.DoActionAsync(act);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task<GameStatusResponse> GameStatus(IGame game, string playerId = null)
        {
            try
            {
                var grpcChannel = new Channel(_config.Solarium.SolariumHostAddress, ChannelCredentials.Insecure);
                var client = new Solarium.Solarium.SolariumClient(grpcChannel);

                var req = new GameStatusRequest { 
                    GameID = game.GameId,
                };

                if (playerId != null)
                {
                    var key = (await (await Players(game)).SingleOrDefault(p => p.SolariumId == playerId))?.SecretKey;
                    if (key != null)
                    {
                        req.PlayerID = playerId;
                        req.PlayerSecret = (await (await Players(game)).SingleOrDefault(p => p.SolariumId == playerId))?.SecretKey;
                    }
                }

                return await client.GameStatusAsync(req);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public void InjectGameEvent(string id, IUser user, IMessageChannel channel, string message)
        {
            if (!_handlers.TryGetValue(id, out var value))
                return;

            value.InjectGameEvent(user, channel, message);
        }
        public async Task<IGame> CreateGame(ulong guildId, NewGameRequest.Types.GameMode mode, NewGameRequest.Types.DifficultyLevel difficulty)
        {
            async Task Insert(IGame g)
            {
                try
                {
                    using (var cmd = _database.CreateCommand())
                    {
                        cmd.CommandText = InsertGameSql;
                        cmd.Parameters.Add(new SQLiteParameter("@ChannelId", System.Data.DbType.String) {Value = g.ChannelId.ToString()});
                        cmd.Parameters.Add(new SQLiteParameter("@GuildId", System.Data.DbType.String) {Value = g.GuildId.ToString()});
                        cmd.Parameters.Add(new SQLiteParameter("@GameId", System.Data.DbType.String) {Value = g.GameId});
                        cmd.Parameters.Add(new SQLiteParameter("@Name", System.Data.DbType.String) {Value = g.Name});
                        cmd.Parameters.Add(new SQLiteParameter("@Description", System.Data.DbType.String) {Value = g.Description});
                        cmd.Parameters.Add(new SQLiteParameter("@Mode", System.Data.DbType.String) {Value = mode.ToString()});

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            // Sanity check that this guild exists
            var guild = _client.GetGuild(guildId);
            if (guild == null)
                return null;

            // Check guild is allowed to create solarium games
            var maybeCategory = await GetCategory(guildId);
            if (!maybeCategory.HasValue)
                return null;
            var category = maybeCategory.Value;

            // Ask the remote end to make a new game
            var grpcChannel = new Channel(_config.Solarium.SolariumHostAddress, ChannelCredentials.Insecure);
            var client = new Solarium.Solarium.SolariumClient(grpcChannel);
            var response = await client.NewGameAsync(new NewGameRequest {
                Gamemode = mode,
                Difficulty = difficulty,
            });
            await grpcChannel.ShutdownAsync();

            // Check if a game was successfully created
            if (response.ID == null)
                return null;

            // get game details
            var gameName = response.Name ?? "null_game_name";
            var gameDesc = response.Description;

            // Create channel for this game
            var channel = await guild.CreateTextChannelAsync(gameName, prop => {
                prop.CategoryId = category;
            });
            if (!string.IsNullOrWhiteSpace(gameDesc))
                await channel.SendMessageAsync(gameDesc);

            // Create game
            var game = new Game(channel.Id, guild.Id, response.ID, gameName, gameDesc, mode);

            // Insert game into database, delete channel if that fails
            try
            {
                await Insert(game);
            }
            catch (Exception)
            {
                await channel.DeleteAsync();
                throw;
            }

            // Start a never ending task to display notifications (do not await)
            _ = SubscribeToGameUpdateStream(game, true);

            return game;
        }

        public async Task<IAsyncEnumerable<IGame>> GetGames(ulong? guild = null, ulong? channel = null)
        {
            IGame ParseGame(DbDataReader reader)
            {
                return new Game(
                    ulong.Parse((string)reader["ChannelId"]),
                    ulong.Parse((string)reader["GuildId"]),
                    (string)reader["GameId"],
                    (string)reader["Name"],
                    (string)reader["Description"],
                    Enum.Parse<NewGameRequest.Types.GameMode>((string)reader["Mode"])
                );
            }

            DbCommand PrepareQuery(IDatabaseService db)
            {
                var cmd = db.CreateCommand();
                cmd.CommandText = GetGamesSql;
                cmd.Parameters.Add(new SQLiteParameter("@ChannelId", System.Data.DbType.String) { Value = channel?.ToString() });
                cmd.Parameters.Add(new SQLiteParameter("@GuildId", System.Data.DbType.String) { Value = guild?.ToString() });
                return cmd;
            }

            return new SqlAsyncResult<IGame>(_database, PrepareQuery, ParseGame);
        }

        [NotNull] public async Task DestroyGame(IGame game)
        {
            //todo: tell other end game is over
            //game.GameId

            // Delete channel (if it still exists)
            await ((_client.GetChannel(game.ChannelId) as IGuildChannel)?.DeleteAsync() ?? Task.CompletedTask);

            // Remove from DB
            try
            {
                using (var cmd = _database.CreateCommand())
                {
                    cmd.CommandText = DeleteGameSql;
                    cmd.Parameters.Add(new SQLiteParameter("@GameId", System.Data.DbType.String) {Value = game.GameId});

                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        [NotNull] private IPlayer ParsePlayer([NotNull] DbDataReader reader)
        {
            return new Player(
                _client.GetUser(ulong.Parse((string)reader["DiscordPlayerId"])),
                (string)reader["SecretKey"],
                (string)reader["SolariumPlayerId"]
            );
        }

        private class Game
            : IGame
        {
            public ulong ChannelId { get; }
            public ulong GuildId { get; }
            public string GameId { get; }
            public string Name { get; }
            public string Description { get; }
            public NewGameRequest.Types.GameMode Mode { get; }

            public Game(ulong channelId, ulong guildId, string gameId, string name, string description, NewGameRequest.Types.GameMode mode)
            {
                ChannelId = channelId;
                GuildId = guildId;
                GameId = gameId;
                Name = name;
                Description = description;
                Mode = mode;
            }
        }

        private class Player
            : IPlayer
        {
            public Player(IUser user, string secretKey, string solariumId)
            {
                User = user;
                SecretKey = secretKey;
                SolariumId = solariumId;
            }

            public IUser User { get; }
            public string SecretKey { get; }
            public string SolariumId { get; }
        }
    }
}
