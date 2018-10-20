using System.Threading.Tasks;
using JetBrains.Annotations;
using Steam.Models.SteamCommunity;
using Steam.Models.SteamStore;
using SteamWebAPI2.Interfaces;

namespace Mute.Services
{
    public class SteamApi
    {
        private readonly SteamUser _user;
        private readonly SteamStore _store;
        private readonly SteamUserStats _stats;

        public SteamApi([NotNull] Configuration config)
        {
            _user = new SteamUser(config.Steam.WebApiKey);
            _store = new SteamStore();
            _stats = new SteamUserStats(config.Steam.WebApiKey);
        }

        public async Task<PlayerSummaryModel> GetUserSummary(ulong id)
        {
            var response = await _user.GetPlayerSummaryAsync(id);
            return response.Data;
        }

        public async Task<SteamCommunityProfileModel> GetUserCommunityProfile(ulong id)
        {
            var response = await _user.GetCommunityProfileAsync(id);
            return response;
        }

        public async Task<StoreAppDetailsDataModel> GetStoreAppDetail(uint id)
        {
            var response = await _store.GetStoreAppDetailsAsync(id);
            return response;
        }
    }
}
