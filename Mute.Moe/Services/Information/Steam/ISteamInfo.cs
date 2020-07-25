using System.Collections.Generic;
using System.Threading.Tasks;
using Steam.Models.SteamCommunity;
using Steam.Models.SteamPlayer;
using Steam.Models.SteamStore;

namespace Mute.Moe.Services.Information.Steam
{
    public interface ISteamInfo
    {
        Task<PlayerSummaryModel?> GetUserSummary(ulong userSteamId);

        Task<SteamCommunityProfileModel?> GetUserCommunityProfile(ulong userSteamId);

        Task<UserStatsForGameResultModel?> GetUserStatsForGame(ulong userSteamId, uint appId);

        Task<uint> GetCurrentPlayerCount(uint appId);

        Task<IReadOnlyCollection<OwnedGameModel>?> GetOwnedGames(ulong userSteamId);

        Task<IReadOnlyCollection<RecentlyPlayedGameModel>?> GetRecentlyPlayedGames(ulong userSteamId);

        Task<StoreAppDetailsDataModel?> GetStoreInfo(uint appId);
    }
}
