using Microsoft.Recognizers.Text.DateTime;

namespace Mute.Moe.Utilities;

/// <summary>
/// Helpers for fuzzy parsing of information from strings
/// </summary>
public static class FuzzyParsing
{
    /// <summary>
    /// Extract a range of time
    /// </summary>
    /// <param name="userInput"></param>
    /// <returns></returns>
    public static MomentRangeExtraction MomentRange(string userInput)
    {
        return Extract(userInput)
            ?? new MomentRangeExtraction(
                IsValid: false,
                Value: (DateTime.MinValue, DateTime.MaxValue),
                ErrorMessage: "That doesn't seem to be a valid time range."
            );

        MomentRangeExtraction? Extract(string input)
        {
            // Get DateTime for the specified culture
            var results = DateTimeRecognizer.RecognizeDateTime(input, "en-*", DateTimeOptions.EnablePreview, DateTime.UtcNow);

            //Try to get the date/time range
            var dt = results.FirstOrDefault(d => d.TypeName.StartsWith("datetimeV2.daterange"));

            // ReSharper disable once UseNullPropagation
            if (dt == null)
                return null;

            var resolutionValues = ((IList<Dictionary<string, string>>?)dt.Resolution["values"])?.FirstOrDefault();
            if (resolutionValues == null)
                return null;

            //The time result could be one of several types
            var subType = dt.TypeName.Split('.').Last();

            //Check if it's a date/time, in which case return a range that starts and ends at that time
            if ((subType.Contains("date") && !subType.Contains("range")) || subType.Contains("time"))
            {
                if (!resolutionValues.TryGetValue("value", out var value))
                    return null;
                    
                if (!DateTime.TryParse(value, out var moment))
                    return null;

                moment += ApplyTimezone(resolutionValues);
                return new MomentRangeExtraction(IsValid: true, Value: (moment, moment), ErrorMessage: null);
            }

            //Check if it's a range of times
            if (subType.Contains("date") && subType.Contains("range"))
            {
                if (!resolutionValues.TryGetValue("start", out var valueStart))
                    return null;
                if (!resolutionValues.TryGetValue("end", out var valueEnd))
                    return null;

                if (!DateTime.TryParse(valueStart, out var momentStart))
                    return null;
                if (!DateTime.TryParse(valueEnd, out var momentEnd))
                    return null;

                momentStart += ApplyTimezone(resolutionValues);
                momentEnd += ApplyTimezone(resolutionValues);

                return new MomentRangeExtraction(IsValid: true, Value: (momentStart, momentEnd));
            }

            return null;
        }

        static TimeSpan ApplyTimezone(IReadOnlyDictionary<string, string> values)
        {
            if (values.TryGetValue("utcOffsetMins", out var utcOff))
            {
                var offset = int.Parse(utcOff);
                return -TimeSpan.FromMinutes(offset);
            }

            return TimeSpan.Zero;
        }
    }

    /// <summary>
    /// Result from trying to extract a moment range from a user input
    /// </summary>
    /// <param name="IsValid"></param>
    /// <param name="Value"></param>
    /// <param name="ErrorMessage"></param>
    public record MomentRangeExtraction(bool IsValid, (DateTime Min, DateTime Max) Value, string? ErrorMessage = null);


    /// <summary>
    /// Extract a single point in time
    /// </summary>
    /// <param name="userInput"></param>
    /// <param name="biasNext">If there are multiple resolutions take the first one in the future</param>
    /// <returns></returns>
    public static MomentExtraction Moment(string userInput, bool biasNext = true)
    {
        return Extract(userInput)
            ?? Extract($"in {userInput}")
            ?? new MomentExtraction(
                   IsValid: false,
                   Value: DateTime.MinValue,
                   ErrorMessage: "That doesn't seem to be a valid moment."
               );

        MomentExtraction? Extract(string input)
        {
            // Get DateTime for the specified culture
            var results = DateTimeRecognizer.RecognizeDateTime(input, "en-*", DateTimeOptions.EnablePreview, DateTime.UtcNow);

            // Try to get the date/time
            var dt = results.FirstOrDefault(d => d.TypeName.StartsWith("datetimeV2.datetime"));
            if (dt == null)
            {
                var d = results.FirstOrDefault(d => d.TypeName is "datetimeV2.datetime" or "datetimeV2.date");
                if (d == null)
                    return null;

                dt = d;
            }

            var values = (IList<Dictionary<string, string>>?)dt.Resolution["values"];
            if (values == null || values.Count == 0)
                return null;

            // Select a resolution
            Dictionary<string, string> resolutionValues;
            if (biasNext)
            {
                var r = (from v in values
                         let date = DateTime.Parse(v["value"])
                         where date > DateTime.UtcNow
                         orderby date
                         select v).FirstOrDefault();

                if (r == null)
                    return null;
                resolutionValues = r;
            }
            else
            {
                resolutionValues = values.First();
            }

            //The time result could be one of several types
            var subType = dt.TypeName.Split('.').Last();

            //Check if it's a date/time, but not a range
            if ((subType.Contains("date") && !subType.Contains("range")) || subType.Contains("time"))
            {
                if (!resolutionValues.TryGetValue("value", out var value))
                    return null;

                if (!DateTime.TryParse(value, out var moment))
                    return null;

                moment = DateTime.SpecifyKind(moment + ApplyTimezone(resolutionValues), DateTimeKind.Utc);
                return new MomentExtraction(IsValid: true, Value: moment);
            }

            return null;
        }

        static TimeSpan ApplyTimezone(IReadOnlyDictionary<string, string> values)
        {
            // If there is set then there's a timezone, but it's ambiguous
            if (values.TryGetValue("timezone", out var tz) && tz == "UTC+XX:XX")
            {
                // Try to fix up some special cases
                if (values.TryGetValue("timezoneText", out var tztext))
                {
                    return tztext switch {
                        "bst" => TimeSpan.FromHours(-1),
                        _ => TimeSpan.Zero,
                    };
                }

                return TimeSpan.Zero;
            }

            if (values.TryGetValue("utcOffsetMins", out var utcOff))
            {
                var offset = int.Parse(utcOff);
                return -TimeSpan.FromMinutes(offset);
            }

            return TimeSpan.Zero;
        }
    }

    /// <summary>
    /// Result from trying to extract a single moment from user input
    /// </summary>
    /// <param name="IsValid"></param>
    /// <param name="Value"></param>
    /// <param name="ErrorMessage"></param>
    public record MomentExtraction(bool IsValid, DateTime Value, string? ErrorMessage = null);
}