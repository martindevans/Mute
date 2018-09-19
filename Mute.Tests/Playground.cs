using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DateTime;
using Microsoft.Recognizers.Text.DateTime.English;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mute.Tests
{
    [TestClass]
    public class Playground
    {
        [TestMethod]
        public void MethodName()
        {
            var extraction = DateTimeRecognizer.RecognizeDateTime("Remind me at 17:30 shanghai time", Culture.English, DateTimeOptions.EnablePreview);
            foreach (var modelResult in extraction)
            {
                Console.WriteLine(modelResult.Text);
                foreach (var resolution in modelResult.Resolution)
                {
                    Console.WriteLine(resolution.Key);
                    if (resolution.Value is List<Dictionary<string, string>> dicts)
                    {
                        foreach (var dict in dicts)
                        foreach (var keyValuePair in dict)
                        {
                            Console.WriteLine(" - " + keyValuePair.Key + " " + keyValuePair.Value);
                        }
                            
                    }
                    else
                        Console.WriteLine(" - " + resolution.Value.GetType().FullName);
                }
                Console.WriteLine();
            }

        }

        [TestMethod]
        public void MethodName2()
        {
            var extr = ValidateAndExtract("Remind me at 19:35 shanghai time tomorrow");
            if (extr.IsValid)
                Console.WriteLine(extr.Values.Single());
            else
                Console.WriteLine(extr.ErrorMessage);

            var extr2 = ValidateAndExtract("Remind me in 10 minutes");
            if (extr2.IsValid)
                Console.WriteLine(extr2.Values.Single());
            else
                Console.WriteLine(extr2.ErrorMessage);

            var extr3 = ValidateAndExtract("Remind me tomorrow morning");
            if (extr3.IsValid)
                Console.WriteLine(extr3.Values.Single());
            else
                Console.WriteLine(extr3.ErrorMessage);

            var extr4 = ValidateAndExtract("Remind me at 19:35");
            if (extr4.IsValid)
                Console.WriteLine(extr4.Values.Single());
            else
                Console.WriteLine(extr4.ErrorMessage);
        }

        public static Extraction ValidateAndExtract(string input)
        {
            Extraction Extract()
            {
                // Get DateTime for the specified culture
                var results = DateTimeRecognizer.RecognizeDateTime(input, Culture.English, DateTimeOptions.EnablePreview, DateTime.UtcNow);

                //Try to get the date/time
                var dt = results.FirstOrDefault(d => d.TypeName.StartsWith("datetimeV2"));
                if (dt == null)
                    return null;

                var resolutionValues = (IList<Dictionary<string, string>>)dt.Resolution["values"];

                //The time result could be one of several types
                var subType = dt.TypeName.Split('.').Last();

                //Check if it's a date/time, but not a range
                if (subType.Contains("date") && !subType.Contains("range"))
                {
                    var moment = resolutionValues.Select(v => DateTime.Parse(v["value"])).FirstOrDefault();
                    return new Extraction {IsValid = true, Values = new[] {moment}};
                }

                //Check if it's a range of times, in which case return the start of the range
                if (subType.Contains("date") && subType.Contains("range"))
                {
                    var from = DateTime.Parse(resolutionValues.First()["start"]);

                    return new Extraction {IsValid = true, Values = new[] {from}};
                }

                //Check if it's just a plain time
                if (subType.Contains("time"))
                {
                    var values = resolutionValues.FirstOrDefault();
                    if (values == null)
                        return null;

                    if (!values.TryGetValue("value", out var value))
                        return null;

                    var moment = DateTime.Parse(value);

                    if (values.TryGetValue("utcOffsetMins", out var utcOff))
                    {
                        var offset = int.Parse(utcOff);
                        moment -= TimeSpan.FromMinutes(offset);
                    }

                    return new Extraction {IsValid = true, Values = new[] {moment}};
                }

                return null;
            }

            return Extract() ?? new Extraction
            {
                IsValid = false,
                Values = Enumerable.Empty<DateTime>(),
                ErrorMessage = "I'm sorry, that doesn't seem to be a valid delivery date and time"
            };
        }

        public class Extraction
        {
            public bool IsValid { get; set; }

            public IEnumerable<DateTime> Values { get; set; }

            public string ErrorMessage { get; set; }
        }
    }
}
