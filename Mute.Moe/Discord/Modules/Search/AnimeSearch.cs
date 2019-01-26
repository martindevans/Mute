using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Services.Information.Anime;

namespace Mute.Moe.Discord.Modules.Search
{
    public class AnimeSearch
        : BaseModule
    {
        private readonly IAnimeInfo _animeSearch;

        public AnimeSearch(IAnimeInfo animeSearch)
        {
            _animeSearch = animeSearch;
        }

        [Command("anime"), Summary("I will tell you about the given anime")]
        [TypingReply]
        public async Task FindAnime([Remainder] string term)
        {
            var anime = await _animeSearch.GetAnimeInfoAsync(term);

            if (anime == null)
                await TypingReplyAsync("I can't find an anime by that name");
            else
            {
                var nsfwOk = (!(Context.Channel is ITextChannel tc)) || tc.IsNsfw;
                if (anime.Adult && !nsfwOk)
                {
                    await TypingReplyAsync($"I found a NSFW anime called `{anime.TitleEnglish}` but NSFW content is not allowed in this channel");
                    return;
                }

                var desc = anime.Description ?? "";
                if (desc.Length > 2048)
                {
                    var addon = "...";
                    if (!string.IsNullOrWhiteSpace(anime.Url))
                        addon += $"... <[Read More]({anime.Url})>";

                    desc = desc.Substring(0, 2047 - addon.Length);
                    desc += addon;
                }

                var builder = new EmbedBuilder()
                      .WithDescription(desc)
                      .WithColor(anime.Adult ? Color.DarkPurple : Color.Blue)
                      .WithImageUrl(anime.ImageUrl ?? "");

                if (anime.TitleJapanese != null && anime.TitleEnglish != null)
                    builder = builder.WithAuthor(anime.TitleJapanese, url: anime.Url).WithTitle(anime.TitleEnglish);
                else if (anime.TitleEnglish != null ^ anime.TitleJapanese != null)
                    builder = builder.WithTitle(anime.TitleEnglish ?? anime.TitleJapanese).WithUrl(anime.Url);
                else
                    builder = builder.WithTitle("Unknown Title").WithUrl(anime.Url);

                //Extract a string describing dates
                string dateString = null;
                if (anime.StartDate.HasValue && anime.EndDate.HasValue)
                    dateString = $"{anime.StartDate.Value.UtcDateTime:dd-MMM-yyyy} -> {anime.EndDate.Value.UtcDateTime:dd-MMM-yyyy}";
                else if (anime.StartDate.HasValue)
                    dateString = $"Started airing {anime.StartDate.Value.UtcDateTime:dd-MMM-yyyy}";

                if (anime.TotalEpisodes.HasValue && dateString != null)
                    builder = builder.WithFields(new EmbedFieldBuilder().WithName($"{anime.TotalEpisodes} episode{(anime.TotalEpisodes > 1 ? "s" : "")}").WithValue(dateString));
                else if (anime.TotalEpisodes.HasValue)
                    builder = builder.WithFields(new EmbedFieldBuilder().WithName("Episodes").WithValue(anime.TotalEpisodes.ToString()));
                else if (dateString != null)
                    builder = builder.WithFields(new EmbedFieldBuilder().WithName("Airing Dates").WithValue(dateString));

                await ReplyAsync(builder);
            }
        }
    }
}
