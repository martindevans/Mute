using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.Choice;
using Microsoft.Recognizers.Text.DateTime;
using Microsoft.Recognizers.Text.NumberWithUnit;

namespace Mute.Moe.Utilities;

public static class FuzzyParsing
{
    public static MomentRangeExtraction MomentRange(string userInput, string culture = Culture.EnglishOthers)
    {
        return Extract(userInput)
            ?? new MomentRangeExtraction
               {
                   IsValid = false,
                   Value = (DateTime.MinValue, DateTime.MaxValue),
                   ErrorMessage = "That doesn't seem to be a valid time range.",
               };

        MomentRangeExtraction? Extract(string input)
        {
            // Get DateTime for the specified culture
            var results = DateTimeRecognizer.RecognizeDateTime(input, culture, DateTimeOptions.EnablePreview, DateTime.UtcNow);

            //Try to get the date/time range
            var dt = results.FirstOrDefault(d => d.TypeName.StartsWith("datetimeV2.daterange"));
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
                return new MomentRangeExtraction { IsValid = true, Value = (moment, moment) };
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

                return new MomentRangeExtraction { IsValid = true, Value = (momentStart, momentEnd) };
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

    public class MomentRangeExtraction
    {
        public bool IsValid { get; set; }

        public (DateTime, DateTime) Value { get; set; }

        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="userInput"></param>
    /// <param name="culture"></param>
    /// <param name="biasNext">If there are multiple resolutions take the first one in the future</param>
    /// <returns></returns>
    public static MomentExtraction Moment(string userInput, string culture = Culture.EnglishOthers, bool biasNext = true)
    {
        return Extract(userInput)
            ?? Extract($"in {userInput}")
            ?? new MomentExtraction
               {
                   IsValid = false,
                   Value = DateTime.MinValue,
                   ErrorMessage = "That doesn't seem to be a valid moment.",
               };

        MomentExtraction? Extract(string input)
        {
            // Get DateTime for the specified culture
            var results = DateTimeRecognizer.RecognizeDateTime(input, culture, DateTimeOptions.EnablePreview, DateTime.UtcNow);

            // Try to get the date/time
            var dt = results.FirstOrDefault(d => d.TypeName.StartsWith("datetimeV2.datetime"));
            if (dt == null)
            {
                var d = results.FirstOrDefault(d => d.TypeName.StartsWith("datetimeV2.date"));
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
                return new MomentExtraction { IsValid = true, Value = moment };
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

    public class MomentExtraction
    {
        public bool IsValid { get; set; }

        public DateTime Value { get; set; }

        public string? ErrorMessage { get; set; }
    }

    public static TimeOffsetExtraction TimeOffset(string input, string culture = Culture.EnglishOthers)
    {
        return Extract() ?? new TimeOffsetExtraction
        {
            IsValid = false,
            UtcOffset = TimeSpan.Zero,
            ErrorMessage = "I'm sorry, that doesn't seem to be a valid timezone",
        };

        TimeOffsetExtraction? Extract()
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
                    ErrorMessage = null,
                };
            }

            return null;
        }
    }

    public class TimeOffsetExtraction
    {
        public bool IsValid { get; set; }

        public TimeSpan UtcOffset { get; set; }

        public string? ErrorMessage { get; set; }
    }

    public static BooleanChoiceExtraction BooleanChoice(string input, string culture = Culture.EnglishOthers)
    {
        var bools = ChoiceRecognizer.RecognizeBoolean(input, culture).Where(a => a.TypeName == "boolean").ToArray();

        var trues = 0;
        var falses = 0;
        float bestConfidence = 0;

        foreach (var item in bools)
        {
            if (!item.Resolution.TryGetValue("value", out var valueObj))
                continue;
            if (!item.Resolution.TryGetValue("score", out var confidenceObj))
                continue;

            var value = Convert.ToBoolean(valueObj);
            var confidence = Convert.ToSingle(confidenceObj);

            //Keep a count of true and false matches
            if (value)
                trues++;
            else
                falses++;

            //Return false as soon as we find both results (i.e. we're not sure)
            if (trues * falses != 0)
                return new BooleanChoiceExtraction(false, 0);

            bestConfidence = Math.Max(bestConfidence, confidence);
        }

        return new BooleanChoiceExtraction(trues > 0, bestConfidence);
    }

    public class BooleanChoiceExtraction
    {
        public bool Value { get; }
        public float Confidence { get; }

        public BooleanChoiceExtraction(bool value, float confidence)
        {
            Value = value;
            Confidence = confidence;
        }
    }

    public static Extraction CurrencyAndAmount(string input, string culture = Culture.EnglishOthers)
    {
        return Extract() ?? new Extraction("value,unit");

        Extraction? Extract()
        {
            var results = NumberWithUnitRecognizer.RecognizeCurrency(input, culture);

            //Try to get the result
            var cur = results.FirstOrDefault(d => d.TypeName.StartsWith("currency"));
            if (cur == null)
                return null;

            var values = cur.Resolution;

            if (!values.TryGetValue("unit", out var unitObj) || unitObj is not string unit)
                return new Extraction("unit");

            if (!values.TryGetValue("value", out var valueObj) || valueObj is not string value)
                return new Extraction("value");

            if (!decimal.TryParse(value, out var deci))
                return new Extraction("value");

            return new Extraction(unit, deci);
        }
    }

    public class Extraction
    {
        public bool IsValid { get; }

        public string? Currency { get; }
        public decimal Amount { get; }

        public string? ErrorMessage { get; }

        public Extraction(string error)
        {
            IsValid = false;
            ErrorMessage = error;

            Currency = null;
            Amount = 0;
        }

        public Extraction(string currency, decimal amount)
        {
            IsValid = true;
            Currency = currency;
            Amount = amount;

            ErrorMessage = null;
        }
    }
}