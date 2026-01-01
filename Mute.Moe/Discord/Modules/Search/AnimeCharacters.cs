using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Services.Information.Anime;

namespace Mute.Moe.Discord.Modules.Search;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

[UsedImplicitly]
public class AnimeCharacters
    : MuteBaseModule
{
    private readonly ICharacterInfo _characters;

    public AnimeCharacters(ICharacterInfo characters)
    {
        _characters = characters;
    }

    [Command("character"), Summary("I will tell you about the given anime character"), Alias("waifu")]
    [TypingReply]
    [UsedImplicitly]
    public Task FindCharacters([Remainder] string term)
    {
        return FindCharacter(1, term);
    }

    [Command("character"), Summary("I will tell you about the given anime character"), Alias("waifu")]
    [TypingReply]
    [UsedImplicitly]
    public async Task FindCharacter(int max, [Remainder] string term)
    {
        await DisplayItemList(
            await _characters.GetCharactersInfoAsync(term).Take(max).ToArrayAsync(),
            "I can't find a character by that name",
            async c => await ReplyAsync(EmbedCharacter(c)),
            l => $"I have found {l.Count} potential characters:",
            (c, i) => $"{i}. {StringCharacter(c)}"
        );
    }

    private static string StringCharacter(ICharacter character)
    {
        return $"{character.FamilyName} {character.GivenName}";
    }

    private static EmbedBuilder EmbedCharacter(ICharacter character)
    {
        var desc = character.Description;
        if (desc.Length > 2048)
        {
            var addon = "...";
            if (!string.IsNullOrWhiteSpace(character.Url))
                addon += $"... <[Read More]({character.Url})>";

            desc = desc[..(2047 - addon.Length)];
            desc += addon;
        }

        var builder = new EmbedBuilder()
            .WithDescription(desc)
            .WithColor(Color.Gold)
            .WithImageUrl(character.ImageUrl)
            .WithAuthor(character.FamilyName + " " + character.GivenName)
            .WithFooter("🦑 https://anilist.co")
            .WithUrl(character.Url);

        return builder;
    }
}