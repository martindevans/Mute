using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using JetBrains.Annotations;
using Solarium;

namespace Mute.Moe.Services.SolariumGame
{
    public interface ISolarium
    {
        [NotNull, ItemCanBeNull] Task<IGame> CreateGame(ulong guild, NewGameRequest.Types.GameMode mode, NewGameRequest.Types.DifficultyLevel difficulty);

        [NotNull, ItemNotNull] Task<IAsyncEnumerable<IGame>> GetGames(ulong? guild = null, ulong? channel = null);

        Task DestroyGame([NotNull] IGame game);


        Task<ulong?> GetCategory(ulong guild);

        Task SetCategory(ulong guild, ulong categoryId);


        [NotNull, ItemNotNull] Task<IAsyncEnumerable<IPlayer>> Players([NotNull] IGame game);

        [NotNull, ItemCanBeNull] Task<IGame> Join(ulong guildId, ulong channelId, ulong userId);

        [NotNull, ItemCanBeNull] Task<DoActionResponse> DoAction(Action<DoActionRequest> action, ulong user, [NotNull] IGame game);

        [NotNull, ItemNotNull] Task<GameStatusResponse> GameStatus([NotNull] IGame game, [CanBeNull] string playerId = null);

        void InjectGameEvent([NotNull] string id, IUser user, [NotNull] IMessageChannel channel, [NotNull] string action);
    }

    public interface IPlayer
    {
        IUser User { get; }

        string SecretKey { get; }

        string SolariumId { get; }
    }

    public interface IGame
    {
        string GameId { get; }

        ulong GuildId { get; }

        ulong ChannelId { get; }

        string Name { get; }

        string Description { get; }

        NewGameRequest.Types.GameMode Mode { get; }
    }
}
