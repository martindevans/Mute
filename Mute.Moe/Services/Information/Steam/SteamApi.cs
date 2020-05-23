using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Steam.Models.SteamCommunity;
using Steam.Models.SteamPlayer;
using SteamWebAPI2.Interfaces;
using SteamWebAPI2.Utilities;

namespace Mute.Moe.Services.Information.Steam
{
    public class SteamApi
        : ISteamInfo
    {
        private readonly SteamUser _user;
        private readonly SteamUserStats _stats;
        private readonly PlayerService _players;

        public SteamApi( Configuration config,  HttpClient http)
        {
            var factory = new SteamWebInterfaceFactory(config.Steam.WebApiKey);

            _user = factory.CreateSteamWebInterface<SteamUser>(http);
            _stats = factory.CreateSteamWebInterface<SteamUserStats>(http);
            _players = factory.CreateSteamWebInterface<PlayerService>(http);
        }

        public async Task<IReadOnlyCollection<OwnedGameModel>?> GetOwnedGames(ulong userSteamId)
        {
            var response = await _players.GetOwnedGamesAsync(userSteamId, true, true);
            return response.Data.OwnedGames;
        }

        public async Task<IReadOnlyCollection<RecentlyPlayedGameModel>?> GetRecentlyPlayedGames(ulong userSteamId)
        {
            var response = await _players.GetRecentlyPlayedGamesAsync(userSteamId);
            return response.Data.RecentlyPlayedGames;
        }

        public async Task<PlayerSummaryModel?> GetUserSummary(ulong userSteamId)
        {
            var response = await _user.GetPlayerSummaryAsync(userSteamId);
            return response.Data;
        }

        public async Task<SteamCommunityProfileModel?> GetUserCommunityProfile(ulong userSteamId)
        {
            var response = await _user.GetCommunityProfileAsync(userSteamId);
            return response;
        }

        public async Task<UserStatsForGameResultModel?> GetUserStatsForGame(ulong userSteamId, uint appId)
        {
            var result = await _stats.GetUserStatsForGameAsync(userSteamId, appId);
            return result.Data;
        }

        public async Task<uint> GetCurrentPlayerCount(uint appId)
        {
            var response = await _stats.GetNumberOfCurrentPlayersForGameAsync(appId);
            return response.Data;
        }
    }
}
