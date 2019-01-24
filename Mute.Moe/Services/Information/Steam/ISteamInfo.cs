using System.Threading.Tasks;
using JetBrains.Annotations;
using Steam.Models.SteamCommunity;
using Steam.Models.SteamPlayer;
using Steam.Models.SteamStore;

namespace Mute.Moe.Services.Information.Steam
{
    public interface ISteamInfo
    {
        [NotNull, ItemCanBeNull] Task<PlayerSummaryModel> GetUserSummary(ulong userSteamId);

        [NotNull, ItemCanBeNull] Task<SteamCommunityProfileModel> GetUserCommunityProfile(ulong userSteamId);

        [NotNull, ItemCanBeNull] Task<UserStatsForGameResultModel> GetUserStatsForGame(ulong userSteamId, uint appId);

        [NotNull, ItemCanBeNull] Task<StoreAppDetailsDataModel> GetStoreAppDetail(uint appId);

        [NotNull] Task<uint> GetCurrentPlayerCount(uint appId);
    }
}
