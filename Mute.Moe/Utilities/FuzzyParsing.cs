using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.Choice;
using Microsoft.Recognizers.Text.DateTime;
using Microsoft.Recognizers.Text.NumberWithUnit;

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
    /// <param name="culture"></param>
    /// <returns></returns>
    public static MomentRangeExtraction MomentRange(string userInput, string culture = Culture.EnglishOthers)
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
            var results = DateTimeRecognizer.RecognizeDateTime(input, culture, DateTimeOptions.EnablePreview, DateTime.UtcNow);

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
    /// <param name="culture"></param>
    /// <param name="biasNext">If there are multiple resolutions take the first one in the future</param>
    /// <returns></returns>
    public static MomentExtraction Moment(string userInput, string culture = Culture.EnglishOthers, bool biasNext = true)
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


    /// <summary>
    /// Extract a time offset (i.e. timezone) from user input
    /// </summary>
    /// <param name="input"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    public static TimeOffsetExtraction TimeOffset(string input, string culture = Culture.EnglishOthers)
    {
        return Extract() ?? new TimeOffsetExtraction(
            IsValid: false,
            UtcOffset: TimeSpan.Zero,
            ErrorMessage: "I'm sorry, that doesn't seem to be a valid timezone"
        );

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

                return new TimeOffsetExtraction(IsValid: true, UtcOffset: TimeSpan.FromMinutes(int.Parse(utcOff)));
            }

            return null;
        }
    }

    /// <summary>
    /// Result from trying to extract a timeoffset from user input
    /// </summary>
    /// <param name="IsValid"></param>
    /// <param name="UtcOffset"></param>
    /// <param name="ErrorMessage"></param>
    public record TimeOffsetExtraction(bool IsValid, TimeSpan UtcOffset, string? ErrorMessage = null);


    /// <summary>
    /// Extract a boolean choice from user input
    /// </summary>
    /// <param name="input"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
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

            // Keep a count of true and false matches
            if (value)
                trues++;
            else
                falses++;

            // Return false as soon as we find both results (i.e. we're not sure)
            if (trues * falses != 0)
                return new BooleanChoiceExtraction(false, 0);

            bestConfidence = Math.Max(bestConfidence, confidence);
        }

        return new BooleanChoiceExtraction(trues > 0, bestConfidence);
    }

    /// <summary>
    /// Result from trying to extract a boolean choice from user input
    /// </summary>
    /// <param name="Value"></param>
    /// <param name="Confidence"></param>
    public record BooleanChoiceExtraction(bool Value, float Confidence);


    /// <summary>
    /// Extract a currency and an amount in that currency from user input
    /// </summary>
    /// <param name="input"></param>
    /// <param name="culture"></param>
    /// <returns></returns>
    public static CurrencyAndAmountExtraction CurrencyAndAmount(string input, string culture = Culture.EnglishOthers)
    {
        return Extract() ?? CurrencyAndAmountExtraction.CreateError("value,unit");

        CurrencyAndAmountExtraction? Extract()
        {
            var results = NumberWithUnitRecognizer.RecognizeCurrency(input, culture);

            //Try to get the result
            var cur = results.FirstOrDefault(d => d.TypeName.StartsWith("currency"));
            if (cur == null)
                return null;

            var values = cur.Resolution;

            if (!values.TryGetValue("unit", out var unitObj) || unitObj is not string unit)
                return CurrencyAndAmountExtraction.CreateError("unit");

            if (!values.TryGetValue("value", out var valueObj) || valueObj is not string value)
                return CurrencyAndAmountExtraction.CreateError("value");

            if (!decimal.TryParse(value, out var deci))
                return CurrencyAndAmountExtraction.CreateError("value");

            return CurrencyAndAmountExtraction.CreateValid(unit, deci);
        }
    }

    /// <summary>
    /// Result from trying to extract currency and amount from user input
    /// </summary>
    public record CurrencyAndAmountExtraction
    {
        /// <summary>
        /// Indicates if this represents a successful extraction
        /// </summary>
        public bool IsValid { get; private init; }

        /// <summary>
        /// The currency unit, null if this is an error state (IsValid == false)
        /// </summary>
        public string? Currency { get; private init; }

        /// <summary>
        /// The currency amount, zero if this is an error state (IsValid == false)
        /// </summary>
        public decimal Amount { get; private init; }

        /// <summary>
        /// Error message, only non-null when IsValid == false
        /// </summary>
        public string? ErrorMessage { get; private init; }

        private CurrencyAndAmountExtraction()
        {
            
        }

        /// <summary>
        /// Create an error state
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        public static CurrencyAndAmountExtraction CreateError(string error)
        {
            return new CurrencyAndAmountExtraction
            {
                IsValid = false,
                ErrorMessage = error,

                Currency = null,
                Amount = 0
            };
        }

        /// <summary>
        /// Create a valid state
        /// </summary>
        /// <param name="currency"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public static CurrencyAndAmountExtraction CreateValid(string currency, decimal amount)
        {
            return new CurrencyAndAmountExtraction
            {
                IsValid = true,
                ErrorMessage = null,

                Currency = currency,
                Amount = amount
            };
        }
    }
}