using System;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.Choice;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mute.Tests
{
    [TestClass]
    public class Playground
    {
        [TestMethod]
        public void MethodName()
        {
            C("yes");
            C("no");
            C("Yes I agree that is an excellent idea");
            C("Yes that's correct");
            C("affirmative");
            C("correct");
            C("I agree");
            C("No that's not it");
        }

        private static void C(string input)
        {
            var extr = ExtractBool(input);

            Console.WriteLine($"{input} : {extr.Value} ({extr.Confidence})");
        }

        [NotNull] private static Extraction ExtractBool(string input, float positiveThreshold = 0.75f, float negativeThreshold = 0.5f)
        {
            var bools = ChoiceRecognizer.RecognizeBoolean(input, Culture.EnglishOthers).Where(a => a.TypeName == "boolean").ToArray();

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

                if (value)
                    trues++;
                else
                    falses++;

                if (trues * falses != 0)
                    return new Extraction(false, 0);
                bestConfidence = Math.Max(bestConfidence, confidence);
            }

            return new Extraction(trues > 0, bestConfidence);
        }

        private class Extraction
        {
            public bool Value { get; }
            public float Confidence { get; }

            public Extraction(bool value, float confidence)
            {
                Value = value;
                Confidence = confidence;
            }
        }
    }
}
