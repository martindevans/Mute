using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DateTime;

namespace Mute
{
    public static class FuzzyParsing
    {
        [NotNull]
        public static MomentExtraction Moment(string input, string culture)
        {
            TimeSpan ApplyTimezone(IReadOnlyDictionary<string, string> values)
            {
                if (values.TryGetValue("utcOffsetMins", out var utcOff))
                {
                    var offset = int.Parse(utcOff);
                    return -TimeSpan.FromMinutes(offset);
                }

                return TimeSpan.Zero;
            }

            MomentExtraction Extract()
            {
                // Get DateTime for the specified culture
                var results = DateTimeRecognizer.RecognizeDateTime(input, culture, DateTimeOptions.EnablePreview, DateTime.UtcNow);

                //Try to get the date/time
                var dt = results.FirstOrDefault(d => d.TypeName.StartsWith("datetimeV2"));
                if (dt == null)
                    return null;

                var resolutionValues = ((IList<Dictionary<string, string>>)dt.Resolution["values"])?.FirstOrDefault();
                if (resolutionValues == null)
                    return null;

                //The time result could be one of several types
                var subType = dt.TypeName.Split('.').Last();

                //Check if it's a date/time, but not a range
                if ((subType.Contains("date") && !subType.Contains("range")) || subType.Contains("time"))
                {
                    if (!resolutionValues.TryGetValue("value", out var value))
                        return null;

                    if (!DateTime.TryParse(value, out var moment))
                        return null;

                    moment += ApplyTimezone(resolutionValues);
                    return new MomentExtraction { IsValid = true, Value = moment };
                }

                //Check if it's a range of times, in which case return the start of the range
                if (subType.Contains("date") && subType.Contains("range"))
                {
                    if (!resolutionValues.TryGetValue("start", out var value))
                        return null;

                    if (!DateTime.TryParse(value, out var moment))
                        return null;

                    moment += ApplyTimezone(resolutionValues);

                    return new MomentExtraction {IsValid = true, Value = moment};
                }

                return null;
            }

            return Extract() ?? new MomentExtraction
            {
                IsValid = false,
                Value = DateTime.MinValue,
                ErrorMessage = "That doesn't seem to be a valid moment."
            };
        }

        public class MomentExtraction
        {
            public bool IsValid { get; set; }

            public DateTime Value { get; set; }

            public string ErrorMessage { get; set; }
        }

        [NotNull]
        public static TimeOffsetExtraction TimeOffset(string input, string culture)
        {
            TimeOffsetExtraction Extract()
            {
                var results = DateTimeRecognizer.RecognizeDateTime(input, culture, DateTimeOptions.EnablePreview);

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
                    
                    return new TimeOffsetExtraction {
                        IsValid = true,
                        UtcOffset = TimeSpan.FromMinutes(int.Parse(utcOff)),
                        ErrorMessage = null
                    };
                }

                return null;
            }

            return Extract() ?? new TimeOffsetExtraction
            {
                IsValid = false,
                UtcOffset = TimeSpan.Zero,
                ErrorMessage = "I'm sorry, that doesn't seem to be a valid timezone"
            };
        }

        public class TimeOffsetExtraction
        {
            public bool IsValid { get; set; }

            public TimeSpan UtcOffset { get; set; }

            public string ErrorMessage { get; set; }
        }
    }
}
