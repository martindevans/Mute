using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Steam.Models.SteamCommunity;
using Steam.Models.SteamPlayer;

namespace Mute.Moe.Services.Information.Steam
{
    public interface ISteamInfo
    {
        [NotNull, ItemCanBeNull] Task<PlayerSummaryModel> GetUserSummary(ulong userSteamId);

        [NotNull, ItemCanBeNull] Task<SteamCommunityProfileModel> GetUserCommunityProfile(ulong userSteamId);

        [NotNull, ItemCanBeNull] Task<UserStatsForGameResultModel> GetUserStatsForGame(ulong userSteamId, uint appId);

        [NotNull] Task<uint> GetCurrentPlayerCount(uint appId);

        [NotNull, ItemCanBeNull] Task<IReadOnlyCollection<OwnedGameModel>> GetOwnedGames(ulong userSteamId);

        [NotNull, ItemCanBeNull] Task<IReadOnlyCollection<RecentlyPlayedGameModel>> GetRecentlyPlayedGames(ulong userSteamId);
    }
}
