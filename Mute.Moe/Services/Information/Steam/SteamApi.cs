using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using FluidCaching;
using Steam.Models.SteamCommunity;
using Steam.Models.SteamPlayer;
using Steam.Models.SteamStore;
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
        private readonly SteamStore _store;

        private readonly FluidCache<StoreAppDetailsDataModel> _steamStoreCache;
        private readonly IIndex<uint, StoreAppDetailsDataModel> _steamStoreCacheById;

        private readonly FluidCache<RecentlyPlayedGamesCacheItem> _recentlyPlayedGames;
        private readonly IIndex<ulong, RecentlyPlayedGamesCacheItem> _recentlyPlayedGamesById;
        
        public SteamApi(Configuration config, HttpClient http)
        {
            var factory = new SteamWebInterfaceFactory(config.Steam?.WebApiKey ?? throw new ArgumentNullException(nameof(config.Steam.WebApiKey)));

            _user = factory.CreateSteamWebInterface<SteamUser>(http);
            _stats = factory.CreateSteamWebInterface<SteamUserStats>(http);
            _players = factory.CreateSteamWebInterface<PlayerService>(http);
            _store = factory.CreateSteamStoreInterface(http);

            _steamStoreCache = new FluidCache<StoreAppDetailsDataModel>(
                1024,
                TimeSpan.FromSeconds(60),
                TimeSpan.FromDays(30),
                () => DateTime.UtcNow
            );
            _steamStoreCacheById = _steamStoreCache.AddIndex("byId", a => a.SteamAppId);

            _recentlyPlayedGames = new FluidCache<RecentlyPlayedGamesCacheItem>(
                128,
                TimeSpan.FromSeconds(60),
                TimeSpan.FromHours(1),
                () => DateTime.UtcNow
            );
            _recentlyPlayedGamesById = _recentlyPlayedGames.AddIndex("byId", a => a.Id);
        }

        public async Task<IReadOnlyCollection<OwnedGameModel>?> GetOwnedGames(ulong userSteamId)
        {
            var response = await _players.GetOwnedGamesAsync(userSteamId, true, true);
            return response.Data.OwnedGames;
        }

        public async Task<IReadOnlyCollection<RecentlyPlayedGameModel>?> GetRecentlyPlayedGames(ulong userSteamId)
        {
            var r = await _recentlyPlayedGamesById.GetItem(userSteamId, async id => {
                var response = await _players.GetRecentlyPlayedGamesAsync(id);
                return new RecentlyPlayedGamesCacheItem(id, response.Data);
            });

            return r?.Data.RecentlyPlayedGames;
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

        public async Task<StoreAppDetailsDataModel?> GetStoreInfoSlow(uint appId)
        {
            return await _steamStoreCacheById.GetItem(appId, async id => {
                try
                {
                    // Add in a very large delay per item to prevent rate limit cutting in and returning null values
                    await Task.Delay(1500);

                    return await _store.GetStoreAppDetailsAsync(id);
                }
                catch
                {
                    // Steam returns a null item which crashes the deserialiser when the rate limit cuts in.
                    // Assuming that's the cause for this exception add another very long delay to satisfy the rate limiter.
                    await Task.Delay(1500);

                    return null!;
                }
            });
        }

        private class RecentlyPlayedGamesCacheItem
        {
            public RecentlyPlayedGamesCacheItem(ulong id, RecentlyPlayedGamesResultModel data)
            {
                Id = id;
                Data = data;
            }

            public ulong Id { get; }
            public RecentlyPlayedGamesResultModel Data { get; }
        }
    }
}
