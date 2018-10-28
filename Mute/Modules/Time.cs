using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using JetBrains.Annotations;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DateTime;
using Mute.Services.Responses.Eliza;
using Mute.Services.Responses.Eliza.Engine;

namespace Mute.Modules
{
    public class Time
        : BaseModule, IKeyProvider
    {
        [Command("time"), Summary("I will tell you the time")]
        public async Task TimeAsync([Remainder, CanBeNull] string tz = null)
        {
            await TypingReplyAsync(await GetTime(tz));
        }

        [ItemNotNull] private async Task<string> GetTime([CanBeNull] string tz = null)
        {
            var extract = ValidateAndExtract(tz ?? "");
            var offset = extract.IsValid ? extract.UtcOffset : TimeSpan.Zero;

            string FormatTime(DateTime dt) => (dt).ToString("HH:mm:ss tt");

            if (extract.IsValid || tz == null)
                return $"The time is {FormatTime(DateTime.UtcNow + offset)} UTC{offset.Hours:+00;-00;+00}:{offset.Minutes:00}";
            else
                return $"I'm not sure what timezone you mean, assuming UTC it's {FormatTime(DateTime.UtcNow)}";
        }

        #region parsing
        [NotNull]
        public static Extraction ValidateAndExtract(string input)
        {
            Extraction Extract()
            {
                var results = DateTimeRecognizer.RecognizeDateTime(input, Culture.English, DateTimeOptions.EnablePreview);

                var dt = results.FirstOrDefault(d => d.TypeName.StartsWith("datetimeV2"));
                if (dt == null)
                    return null;

                var resolutionValues = (IList<Dictionary<string, string>>)dt.Resolution["values"];
                var subType = dt.TypeName.Split('.').Last();

                if (subType.Contains("timezone"))
                {
                    var values = resolutionValues.FirstOrDefault();
                    if (values == null)
                        return null;

                    if (!values.TryGetValue("utcOffsetMins", out var utcOff))
                        return null;

                    // https://github.com/Microsoft/Recognizers-Text/issues/871
                    if (utcOff == "-10000")
                        return null;
                    
                    return new Extraction {
                        IsValid = true,
                        UtcOffset = TimeSpan.FromMinutes(int.Parse(utcOff)),
                        ErrorMessage = null
                    };
                }

                return null;
            }

            return Extract() ?? new Extraction
            {
                IsValid = false,
                UtcOffset = TimeSpan.Zero,
                ErrorMessage = "I'm sorry, that doesn't seem to be a valid timezone"
            };
        }

        public class Extraction
        {
            public bool IsValid { get; set; }

            public TimeSpan UtcOffset { get; set; }

            public string ErrorMessage { get; set; }
        }
        #endregion

        public IEnumerable<Key> Keys
        {
            get
            {
                yield return new Key("time", 10,
                    new Decomposition("what * time in *", d => GetTime(d[1])),
                    new Decomposition("what * time * in *", d => GetTime(d[2])),
                    new Decomposition("what is * time", _ => GetTime()),
                    new Decomposition("what time * in * *", d => GetTime(d[1])),
                    new Decomposition("what time * in *", d => GetTime(d[1])),
                    new Decomposition("what time *", _ => GetTime())
                );
            }
        }
    }
}
