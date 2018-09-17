using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;
using JetBrains.Annotations;
using Mute.Extensions;
using Mute.Services;
using Mute.Services.Responses.Eliza;
using Mute.Services.Responses.Eliza.Engine;

namespace Mute.Modules
{
    public class Time
        : ModuleBase, IKeyProvider
    {
        private readonly TimeService _time;

        public Time(TimeService time)
        {
            _time = time;
        }

        [Command("time"), Summary("I will tell you the time")]
        public async Task TimeAsync([CanBeNull] string tz = null)
        {
            await this.TypingReplyAsync(GetTime(tz));
        }

        [NotNull] private string GetTime([CanBeNull] string tz = null)
        {
            string FormatTime(DateTime? dt = null) => (dt ?? DateTime.UtcNow).ToString("HH:mm:ss tt");

            if (tz == null)
                return $"The time is {FormatTime()} (Zulu/UTC)";

            var t = _time.TimeNow(tz);
            if (t != null)
                return $"It's {FormatTime(t)}";
            
            return $"I don't know the timezone {tz}. Assuming Zulu/UTC it's {FormatTime()}";
        }

        public IEnumerable<Key> Keys
        {
            get
            {
                yield return new Key("time", 10,
                    new Decomposition("what * time in *", false, true, d => GetTime(d[1])),
                    new Decomposition("what * time * in *", false, true, d => GetTime(d[2])),
                    new Decomposition("what is * time", false, true, _ => GetTime()),
                    new Decomposition("what time * in * *", false, true, d => GetTime(d[1])),
                    new Decomposition("what time * in *", false, true, d => GetTime(d[1])),
                    new Decomposition("what time *", false, true, _ => GetTime())
                );
            }
        }
    }
}
