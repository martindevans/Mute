using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Services.Information.Anime;

namespace Mute.Moe.Discord.Modules.Search
{
    public class CharacterSearch
        : BaseModule
    {
        private readonly ICharacterInfo _characters;

        public CharacterSearch(ICharacterInfo characters)
        {
            _characters = characters;
        }

        [Command("character"), Summary("I will tell you about the given anime character"), Alias("waifu")]
        [TypingReply]
        public async Task FindAnime([Remainder] string term)
        {
            var character = await _characters.GetCharacterInfoAsync(term);

            if (character == null)
                await TypingReplyAsync("I can't find a character by that name");
            else
            {
                var desc = character.Description ?? "";
                if (desc.Length > 2048)
                {
                    var addon = "...";
                    if (!string.IsNullOrWhiteSpace(character.Url))
                        addon += $"... <[Read More]({character.Url})>";

                    desc = desc.Substring(0, 2047 - addon.Length);
                    desc += addon;
                }

                var builder = new EmbedBuilder()
                                  .WithDescription(desc)
                                  .WithColor(Color.Gold)
                                  .WithImageUrl(character.ImageUrl ?? "")
                                  .WithAuthor(character.FamilyName + " " + character.GivenName)
                                  .WithFooter("🦑 https://anilist.co")
                                  .WithUrl(character.Url ?? "");


                await ReplyAsync(builder);
            }
        }
    }
}
