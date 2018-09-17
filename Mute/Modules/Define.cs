using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using Mute.Extensions;
using Mute.Services;
using Mute.Services.Responses.Eliza.Topics;

namespace Mute.Modules
{
    public class Define
        : InteractiveBase //, ITopic
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

            if (count == 0)
            {
                await this.TypingReplyAsync("I don't know anything about that, sorry");
            }
            else if (count == 1)
            {
                await this.TypingReplyAsync(definition.Search.Single().Description);
            }
            else
            {
                await this.TypingReplyAsync($"I have found {count} possible items, could you be more specific?");

                var r = await NextMessageAsync(true, true, TimeSpan.FromSeconds(10));
                if (r == null)
                    return;

                var rr = r.Content.ToLowerInvariant();
                if (rr.Contains("list") || rr.Contains("what"))
                {
                    foreach (var item in definition.Search)
                        await this.TypingReplyAsync($" - {item.Description ?? item.Title ?? item.Label} ([{item.Id}]({item.Uri}))");
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
