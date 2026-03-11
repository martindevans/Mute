using Discord;
using Mute.Moe.Services.Information.Anime;

namespace Mute.Moe.Extensions;

/// <summary>
/// Extensions for <see cref="IAnime"/>
/// </summary>
public static class IAnimeExtensions
{
    /// <param name="this"></param>
    extension(IAnime @this)
    {
        /// <summary>
        /// Convert to an embed with summary info about this anime
        /// </summary>
        /// <param name="embed"></param>
        /// <returns></returns>
        public EmbedBuilder ToEmbed(EmbedBuilder? embed = null)
        {
            if (embed == null)
                embed = new EmbedBuilder();

            // Limit description length to 2048 characters
            var desc = @this.Description;
            if (desc.Length > 2048)
            {
                var addon = "...";
                if (!string.IsNullOrWhiteSpace(@this.Url))
                    addon += $"... <[Read More]({@this.Url})>";

                desc = desc[..(2047 - addon.Length)];
                desc += addon;
            }

            // Build embed basics
            var builder = embed
                         .WithDescription(desc)
                         .WithColor(@this.Adult ? Color.DarkPurple : Color.Blue)
                         .WithImageUrl(@this.ImageUrl)
                         .WithFooter("🦑 https://anilist.co")
                         .WithUrl(@this.Url);

            // Attach appropriate title
            if (@this is { TitleJapanese: not null, TitleEnglish: not null })
                builder.WithAuthor(@this.TitleJapanese).WithTitle(@this.TitleEnglish);
            else if ((@this.TitleEnglish != null) ^ (@this.TitleJapanese != null))
                builder.WithTitle(@this.TitleEnglish ?? @this.TitleJapanese);
            else
                builder.WithTitle($"Anime ID: {@this.Id}");

            // Extract a string describing dates
            string? dateString = null;
            if (@this is { StartDate: not null, EndDate: not null })
                dateString = $"{@this.StartDate.Value.UtcDateTime:dd-MMM-yyyy} -> {@this.EndDate.Value.UtcDateTime:dd-MMM-yyyy}";
            else if (@this.StartDate.HasValue)
                dateString = $"Started airing {@this.StartDate.Value.UtcDateTime:dd-MMM-yyyy}";

            // Attach episode info
            if (@this.TotalEpisodes.HasValue && dateString != null)
                builder.WithFields(new EmbedFieldBuilder().WithName($"{@this.TotalEpisodes} episode{(@this.TotalEpisodes > 1 ? "s" : "")}").WithValue(dateString));
            else if (@this.TotalEpisodes.HasValue)
                builder.WithFields(new EmbedFieldBuilder().WithName("Episodes").WithValue(@this.TotalEpisodes.ToString()));
            else if (dateString != null)
                builder.WithFields(new EmbedFieldBuilder().WithName("Airing Dates").WithValue(dateString));

            return builder;
        }

        /// <summary>
        /// Get the full title of this anime
        /// </summary>
        /// <param name="preferJapanese"></param>
        /// <param name="softMaxLength"></param>
        /// <returns></returns>
        public string FullTitle(bool preferJapanese = false, int softMaxLength = int.MaxValue)
        {
            // Attach appropriate title
            if (@this is { TitleJapanese: string jp, TitleEnglish: string en })
            {
                var (preferred, secondary) = preferJapanese ? (jp, en) : (en, jp);

                return preferred.Length < softMaxLength 
                    ? $"{preferred} ({secondary})"
                    : $"{preferred}";
            }

            if (@this.TitleEnglish != null || @this.TitleJapanese != null)
                return (@this.TitleEnglish ?? @this.TitleJapanese)!;
            return $"Anime ID: {@this.Id}";
        }
    }
}