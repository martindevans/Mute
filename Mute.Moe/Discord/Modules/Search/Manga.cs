using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using JetBrains.Annotations;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Services.Information.Anime;

namespace Mute.Moe.Discord.Modules.Search;

[UsedImplicitly]
public class Manga
    : BaseModule
{
    private readonly IMangaInfo _mangaSearch;

    public Manga(IMangaInfo animeSearch)
    {
        _mangaSearch = animeSearch;
    }

    [Command("manga"), Summary("I will tell you about the given manga")]
    [TypingReply]
    public async Task FindManga([Remainder] string term)
    {
        var manga = await _mangaSearch.GetMangaInfoAsync(term);

        if (manga == null)
            await TypingReplyAsync("I can't find an manga by that name");
        else
        {
            var desc = manga.Description;
            if (desc.Length > 2048)
            {
                var addon = "...";
                if (!string.IsNullOrWhiteSpace(manga.Url))
                    addon += $"... <[Read More]({manga.Url})>";

                desc = desc[..(2047 - addon.Length)];
                desc += addon;
            }

            var builder = new EmbedBuilder().WithDescription(desc).WithColor(Color.DarkGreen).WithImageUrl(manga.ImageUrl).WithFooter("🦑 https://anilist.co").WithUrl(manga.Url);

            if (manga is { TitleJapanese: not null, TitleEnglish: not null })
                builder = builder.WithAuthor(manga.TitleJapanese).WithTitle(manga.TitleEnglish);
            else if (manga.TitleEnglish is not null ^ manga.TitleJapanese is not null)
                builder = builder.WithTitle(manga.TitleEnglish ?? manga.TitleJapanese);

            if (manga.Volumes.HasValue)
                builder = builder.WithFields(new EmbedFieldBuilder().WithName("Volumes").WithValue(manga.Volumes.Value));
            if (manga.Chapters.HasValue)
                builder = builder.WithFields(new EmbedFieldBuilder().WithName("Chapters").WithValue(manga.Chapters.Value));

            await ReplyAsync(builder);
        }
    }
}