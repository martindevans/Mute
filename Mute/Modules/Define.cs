using System;
using System.Threading.Tasks;
using Discord.Commands;
using JetBrains.Annotations;
using Mute.Extensions;
using Mute.Services;
using Mute.Services.Responses.Eliza.Eliza;

namespace Mute.Modules
{
    public class Define
        : ModuleBase, ITopic
    {
        private readonly WikipediaService _wikipedia;
        private readonly TimeService _time;

        public Define(WikipediaService wiki, TimeService time)
        {
            _wikipedia = wiki;
            _time = time;
        }

        [Command("define"), Summary("I will briefly explain what a thing is")]
        public async Task DefineAsync([Remainder] string thing)
        {
            var definition = await _wikipedia.GetDefinition(thing, 3);
            await this.TypingReplyAsync(definition ?? "I don't know what that is");
        }

        [Command("time"), Summary("I will tell you the time")]
        public async Task TimeAsync([CanBeNull] string tz = null)
        {
            var time = DateTime.UtcNow;

            if (tz != null)
            {
                var t = _time.TimeNow(tz);
                if (t == null)
                    await this.TypingReplyAsync("Unknown time zone. Assuming Zulu (UTC)");
                else
                    time = t.Value;
            }

            await this.TypingReplyAsync($"The time is {time}");
        }
    }
}
