using System;
using System.IO;
using System.Threading.Tasks;
using FluidCaching;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Mute.Moe.Services.Information.Anime
{
    public class NadekobotAnimeSearch
        : IAnimeInfo
    {
        private readonly IHttpClient _http;

        private readonly IIndex<string, IAnime> _animeById;
        private readonly FluidCache<IAnime> _cache;

        public NadekobotAnimeSearch(IHttpClient http)
        {
            _http = http;

            _cache = new FluidCache<IAnime>(128, TimeSpan.FromHours(1), TimeSpan.FromDays(7), () => DateTime.UtcNow);
            _animeById = _cache.AddIndex("id", a => a.Id);
        }

        public async Task<IAnime> GetAnimeInfoAsync(string search)
        {
            using (var result = await _http.GetAsync("https://aniapi.nadekobot.me/anime/" + Uri.EscapeUriString(search)))
            {
                if (!result.IsSuccessStatusCode)
                    return null;

                JsonAnime anime;
                var serializer = new JsonSerializer();
                using (var sr = new StreamReader(await result.Content.ReadAsStreamAsync()))
                using (var jsonTextReader = new JsonTextReader(sr))
                    anime = serializer.Deserialize<JsonAnime>(jsonTextReader);

                //Before modifying the downloaded data, see if we have it in cache
                var fromCache = await _animeById.GetItem(anime.Id);
                if (fromCache != null)
                    return fromCache;

                //Do cleanup work on downloaded model and store in cache
                anime.ImgUrlLarge = await ValidUrl(anime.ImgUrlLarge);
                anime.ImgUrlMedium = await ValidUrl(anime.ImgUrlMedium);
                anime.ImgUrlSmall = await ValidUrl(anime.ImgUrlSmall);
                anime.Description = anime.Description.Replace("<br>", "");
                _cache.Add(anime);

                return anime;
            }
        }

        [ItemCanBeNull] private async Task<string> ValidUrl(string url)
        {
            var hd = await _http.HeadAsync(url);
            if (!hd.IsSuccessStatusCode)
                return null;
            else
                return url;
        }
    }
}
