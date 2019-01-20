using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mute.Moe.Services.Search;

namespace Mute.Moe.Discord.Modules
{
    public class AnimeSearch
        : BaseModule
    {
        private readonly IAnimeSearch _animeSearch;

        public AnimeSearch(IAnimeSearch animeSearch)
        {
            _animeSearch = animeSearch;
        }

        [Command("anime"), Summary("I will tell you about the given anime")]
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

                //Extract a string describing dates
                string dateString;
                if (anime.StartDate.HasValue && anime.EndDate.HasValue)
                    dateString = $"{anime.StartDate.Value.UtcDateTime:dd-MMM-yyyy} -> {anime.EndDate.Value.UtcDateTime:dd-MMM-yyyy}";
                else if (anime.StartDate.HasValue)
                    dateString = $"Started airing {anime.StartDate.Value.UtcDateTime:dd-MMM-yyyy}";
                else
                    dateString = "";

                //Extract a youtube link
                var url = anime.YoutubeId == null ? null : $"https://www.youtube.com/watch?v={anime.YoutubeId}";

                var builder = new EmbedBuilder()
                              .WithTitle(anime.TitleJapanese)
                              .WithAuthor(anime.TitleEnglish, url: url)
                              .WithDescription(anime.Description)
                              .WithColor(anime.Adult ? Color.DarkPurple : Color.Blue)
                              .WithImageUrl(anime.ImgUrlLarge ?? anime.ImgUrlMedium ?? anime.ImgUrlSmall)
                              .WithFields(new EmbedFieldBuilder()
                                          .WithName($"{anime.TotalEpisodes} episode{(anime.TotalEpisodes > 1 ? "s" : "")}")
                                          .WithValue(dateString));

                await ReplyAsync(builder);
            }
        }
    }
}
