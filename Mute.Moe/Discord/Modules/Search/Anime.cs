using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Extensions;
using Mute.Moe.Services.Information.Anime;

namespace Mute.Moe.Discord.Modules.Search
{
    public class Anime
        : BaseModule
    {
        private readonly IAnimeInfo _animeSearch;

        public Anime(IAnimeInfo animeSearch)
        {
            _animeSearch = animeSearch;
        }

        [Command("anime"), Alias("animu"), Summary("I will tell you about the given anime")]
        [TypingReply]
        public async Task FindAnime([Remainder] string term)
        {
            var anime = await _animeSearch.GetAnimeInfoAsync(term);

            if (anime == null)
            {
                await TypingReplyAsync("I can't find an anime by that name");
                return;
            }
            
            var nsfwOk = Context.Channel is not ITextChannel tc || tc.IsNsfw;
            if (anime.Adult && !nsfwOk)
            {
                await TypingReplyAsync($"I found a NSFW anime called `{anime.TitleEnglish}` but NSFW content is not allowed in this channel");
                return;
            }

            await ReplyAsync(anime.ToEmbed());
            
        }

        [Command("animes"), Alias("animus"), Summary("I will search for anime and display all matches")]
        [TypingReply]
        public async Task FindAnimes([Remainder] string term)
        {
            var nsfwOk = Context.Channel is not ITextChannel tc || tc.IsNsfw;

            var animes = _animeSearch
                .GetAnimesInfoAsync(term)
                .Where(a => !a.Adult || nsfwOk)
                .Select(a => {
                    var title = $"{(a.Adult ? "⚠" : "")} {a.TitleEnglish ?? a.TitleJapanese ?? a.Id}".LimitLength(60);
                    return $"[{title}]({a.Url})";
                })
                .Buffer(12)
                .Select(a => string.Join("\n", a));

            await DisplayLazyPaginatedReply($"Anime Search `{term}`", animes);
        }
    }
}
