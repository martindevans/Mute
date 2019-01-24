using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Mute.Moe.Services.Information.Anime
{
    public interface IAnimeInfo
    {
        [ItemCanBeNull] Task<IAnime> GetAnimeInfoAsync(string search);
    }

    public interface IAnime
    {
        string Id { get; }
        string TitleRomanji { get; }
        string TitleEnglish { get; }
        string TitleJapanese { get; }
        string Type { get; }
        string Description { get; }
        string YoutubeId { get; }

        DateTimeOffset? StartDate { get; }
        DateTimeOffset? EndDate { get; }
        
        bool Adult { get; }

        string ImgUrlSmall { get; }
        string ImgUrlMedium { get; }
        string ImgUrlLarge { get; }

        IReadOnlyList<string> Genres { get; }

        uint TotalEpisodes { get; }
    }

    public class JsonAnime
        : IAnime
    {
        [UsedImplicitly, JsonProperty("id")] public string Id { get; set; }
        [UsedImplicitly, JsonProperty("title_romaji")] public string TitleRomanji { get; set; }
        [UsedImplicitly, JsonProperty("title_english")] public string TitleEnglish { get; set; }
        [UsedImplicitly, JsonProperty("title_japanese")] public string TitleJapanese { get; set; }
        [UsedImplicitly, JsonProperty("type")] public string Type { get; set; }
        [UsedImplicitly, JsonProperty("start_date")] public DateTimeOffset? StartDate { get; set; }
        [UsedImplicitly, JsonProperty("end_date")] public DateTimeOffset? EndDate { get; set; }
        [UsedImplicitly, JsonProperty("description")] public string Description { get; set; }
        [UsedImplicitly, JsonProperty("adult")] public bool Adult { get; set; }

        [UsedImplicitly, JsonProperty("image_url_sml")] public string ImgUrlSmall { get; set; }
        [UsedImplicitly, JsonProperty("image_url_med")] public string ImgUrlMedium { get; set; }
        [UsedImplicitly, JsonProperty("image_url_lge")] public string ImgUrlLarge { get; set; }

        [UsedImplicitly, JsonProperty("genres")] public IReadOnlyList<string> Genres { get; set; }
        [UsedImplicitly, JsonProperty("total_episodes")] public uint TotalEpisodes { get; set; }

        [UsedImplicitly, JsonProperty("youtube_id")] public string YoutubeId { get; set; }
    }
}
