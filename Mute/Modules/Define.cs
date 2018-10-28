using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mute.Services;

namespace Mute.Modules
{
    public class Define
        : BaseModule //, ITopic
    {
        private readonly WikipediaService _wikipedia;

        public Define(WikipediaService wiki)
        {
            _wikipedia = wiki;
        }

        [Command("define"), Summary("I will briefly explain what a thing is")]
        public async Task DefineAsync([Remainder] string thing)
        {
            var definition = await _wikipedia.SearchData(thing);

            var count = definition?.Search?.Count ?? 0;

            if (definition?.Search == null || count == 0)
            {
                await TypingReplyAsync("I don't know anything about that, sorry");
            }
            else if (count == 1)
            {
                await TypingReplyAsync(definition.Search.Single().Description);
            }
            else
            {
                await TypingReplyAsync($"I have found {count} possible items, could you be more specific?");

                var r = await NextMessageAsync(true, true, TimeSpan.FromSeconds(10));
                if (r == null)
                    return;

                var rr = r.Content.ToLowerInvariant();
                if (rr.Contains("list") || rr.Contains("what"))
                {
                    await DisplayItemList(
                        definition.Search,
                        async () => await ReplyAsync("No items :("),
                        async (item, index) => {
                            var embed = new EmbedBuilder()
                                        .WithTitle(item.Label ?? item.Title)
                                        .WithDescription(item.Description)
                                        .WithUrl(item.Uri ?? item.ConceptUri);
                            await TypingReplyAsync(embed);
                        });
                }
            }
        }

        /* #region conversation
        public IEnumerable<string> Keywords => new[] { "define" };

        public ITopicDiscussion TryOpen(IUtterance message)
        {
            return new SingleMessageContext("`Define` context is not implemented yet, sorry :(");
        }
        #endregion */
    }
}
