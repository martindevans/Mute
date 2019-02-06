using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mute.Moe.Services.Information.Wikipedia;

namespace Mute.Moe.Discord.Modules
{
    public class Define
        : BaseModule
    {
        private readonly IWikipedia _wikipedia;

        public Define(IWikipedia wiki)
        {
            _wikipedia = wiki;
        }

        [Command("define"), Summary("I will briefly explain what a thing is")]
        public async Task DefineAsync([Remainder] string thing)
        {
            await DefineAsync(3, thing);
        }

        [Command("define"), Summary("I will briefly explain what a thing is")]
        public async Task DefineAsync(int sentences, [Remainder] string thing)
        {
            //Get definitions from wikipedia
            var definitions = await _wikipedia.Define(thing, sentences: sentences);

            //Define a method to display a single definition in an embed
            Task SingleDefinition(IDefinition def)
            {
                var embed = new EmbedBuilder().WithFooter("📖 wikipedia.org").WithDescription(def.Definition);

                var url = !string.IsNullOrWhiteSpace(def.Url);
                embed = url
                      ? embed.WithAuthor(new EmbedAuthorBuilder().WithName(def.Title).WithUrl(def.Url))
                      : embed.WithTitle(def.Title);

                return ReplyAsync(embed: embed.Build());
            }

            //Display the item(s)
            await DisplayItemList(
                definitions,
                () => "I don't know anything about that, sorry",
                SingleDefinition,
                items => $"I have found {items.Count} possible items, could you be more specific?",
                (item, index) => $"{index + 1}. [{item.Title}]({item.Url})"
            );
        }
    }
}
