using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.Choice;
using Microsoft.Recognizers.Text.DateTime;
using Microsoft.Recognizers.Text.NumberWithUnit;

namespace Mute
{
    public static class FuzzyParsing
    {
        [NotNull] public static MomentExtraction Moment(string input, string culture = Culture.EnglishOthers)
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

        [NotNull] public static TimeOffsetExtraction TimeOffset(string input, string culture = Culture.EnglishOthers)
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

        [NotNull] public static BooleanChoiceExtraction BooleanChoice(string input, string culture = Culture.EnglishOthers)
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

        [NotNull] public static Extraction CurrencyAndAmount(string input, string culture = Culture.EnglishOthers)
        {
            Extraction Extract()
            {
                var results = NumberWithUnitRecognizer.RecognizeCurrency(input, culture);

                //Try to get the result
                var cur = results.FirstOrDefault(d => d.TypeName.StartsWith("currency"));
                if (cur == null)
                    return null;

                var values = cur.Resolution;

                if (!values.TryGetValue("unit", out var unitObj) || !(unitObj is string unit))
                    return new Extraction("unit");

                if (!values.TryGetValue("value", out var valueObj) || !(valueObj is string value))
                    return new Extraction("value");

                if (!decimal.TryParse(value, out var deci))
                    return new Extraction("value");

                return new Extraction(unit, deci);
            }

            return Extract() ?? new Extraction("value,unit");
        }

        public class Extraction
        {
            public bool IsValid { get; }

            public string Currency { get; }
            public decimal Amount { get; }

            public string ErrorMessage { get; }

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
}
