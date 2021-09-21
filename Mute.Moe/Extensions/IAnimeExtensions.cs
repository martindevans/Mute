using Discord;
using Mute.Moe.Services.Information.Anime;

namespace Mute.Moe.Extensions
{
    public static class IAnimeExtensions
    {
        public static EmbedBuilder ToEmbed(this IAnime anime, EmbedBuilder? embed = null)
        {
            if (embed == null)
                embed = new EmbedBuilder();

            // Limit description length to 2048 characters
            var desc = anime.Description;
            if (desc.Length > 2048)
            {
                var addon = "...";
                if (!string.IsNullOrWhiteSpace(anime.Url))
                    addon += $"... <[Read More]({anime.Url})>";

                desc = desc[..(2047 - addon.Length)];
                desc += addon;
            }

            // Build embed basics
            var builder = embed
                  .WithDescription(desc)
                  .WithColor(anime.Adult ? Color.DarkPurple : Color.Blue)
                  .WithImageUrl(anime.ImageUrl)
                  .WithFooter("🦑 https://anilist.co")
                  .WithUrl(anime.Url);

            // Attach appropriate title
            if (anime.TitleJapanese != null && anime.TitleEnglish != null)
                builder.WithAuthor(anime.TitleJapanese).WithTitle(anime.TitleEnglish);
            else if (anime.TitleEnglish != null ^ anime.TitleJapanese != null)
                builder.WithTitle(anime.TitleEnglish ?? anime.TitleJapanese);
            else
                builder.WithTitle($"Anime ID: {anime.Id}");

            // Extract a string describing dates
            string? dateString = null;
            if (anime.StartDate.HasValue && anime.EndDate.HasValue)
                dateString = $"{anime.StartDate.Value.UtcDateTime:dd-MMM-yyyy} -> {anime.EndDate.Value.UtcDateTime:dd-MMM-yyyy}";
            else if (anime.StartDate.HasValue)
                dateString = $"Started airing {anime.StartDate.Value.UtcDateTime:dd-MMM-yyyy}";

            // Attach episode info
            if (anime.TotalEpisodes.HasValue && dateString != null)
                builder.WithFields(new EmbedFieldBuilder().WithName($"{anime.TotalEpisodes} episode{(anime.TotalEpisodes > 1 ? "s" : "")}").WithValue(dateString));
            else if (anime.TotalEpisodes.HasValue)
                builder.WithFields(new EmbedFieldBuilder().WithName("Episodes").WithValue(anime.TotalEpisodes.ToString()));
            else if (dateString != null)
                builder.WithFields(new EmbedFieldBuilder().WithName("Airing Dates").WithValue(dateString));

            return builder;
        }

        public static string FullTitle(this IAnime anime, bool preferJapanese = false, int softMaxLength = int.MaxValue)
        {
            // Attach appropriate title
            if (anime.TitleJapanese != null && anime.TitleEnglish != null)
            {
                var (a, b) = preferJapanese ? (anime.TitleJapanese, anime.TitleEnglish) : (anime.TitleEnglish, anime.TitleJapanese);

                if (a.Length < softMaxLength)
                    return $"{a} ({b})";
                else
                    return $"{a}";
            }
            else if (anime.TitleEnglish != null || anime.TitleJapanese != null)
                return (anime.TitleEnglish ?? anime.TitleJapanese)!;
            else
                return $"Anime ID: {anime.Id}";
        }
    }
}
