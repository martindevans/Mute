using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using JetBrains.Annotations;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Services.Information.UrbanDictionary;
using Mute.Moe.Services.Information.Wikipedia;

namespace Mute.Moe.Discord.Modules.Search;

[UsedImplicitly]
public class Define
    : BaseModule
{
    private readonly IWikipedia _wikipedia;
    private readonly IUrbanDictionary _urban;

    public Define(IWikipedia wiki, IUrbanDictionary urban)
    {
        _wikipedia = wiki;
        _urban = urban;
    }

    [Command("define"), Summary("I will briefly explain what a thing is")]
    [ThinkingReply]
    public async Task DefineAsync([Remainder] string thing)
    {
        await DefineAsync(3, thing);
    }

    [Command("define"), Summary("I will briefly explain what a thing is, within a specified number of sentences")]
    [ThinkingReply]
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
            (item, index) => $"{index + 1}. `{item.Title}` - `{item.Definition}`"
        );
    }

    [Command("urbandefine"), Summary("I will briefly define a word according to urban dictionary")]
    public async Task UrbanDefineAsync([Remainder] string thing)
    {
        var result = await _urban.SearchTermAsync(thing);

        if (result.Count == 0)
        {
            await TypingReplyAsync("I don't know anything about that, sorry");
            return;
        }

        //Select the most upvoted definition
        var best = result.Select(a => new { def = a, score = a.ThumbsUp - a.ThumbsDown }).Aggregate((a, b) => a.score > b.score ? a : b).def;

        //Remove link signifiers
        var definition = best.Definition.Replace("[", "").Replace("]", "");

        //Build an embed card for it
        var embed = new EmbedBuilder().WithFooter("👌 urbandictionary.com").WithDescription(definition).WithTimestamp(best.WrittenOn);
        embed = embed.WithAuthor(new EmbedAuthorBuilder().WithName(best.Word).WithUrl(best.Permalink.ToString()));

        await ReplyAsync(embed: embed.Build());
    }
}