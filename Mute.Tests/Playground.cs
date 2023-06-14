using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using Microsoft.Recognizers.Text.DateTime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Moe.Extensions;
using Mute.Moe.Services.DiceLang.AST;
using Mute.Moe.Utilities;

namespace Mute.Tests
{
    [TestClass]
    public class Playground
    {

        [TestMethod]
        public void MethodName()
        {
            var m = "hello <@!2334>".FindUserMentions();
            Console.WriteLine(m.Single());
        }

        [TestMethod]
        public void BooleanChoices()
        {
            Assert.IsFalse(C("no"));
            Assert.IsFalse(C("No that's not it"));
            Assert.IsFalse(C("no ty"));
            Assert.IsFalse(C("noty"));

            Assert.IsTrue(C("yes"));
            Assert.IsTrue(C("Yes I agree that is an excellent idea"));
            Assert.IsTrue(C("Yes that's correct"));
            Assert.IsTrue(C("I agree"));
            Assert.IsTrue(C("Yep"));

            //Assert.IsTrue(C("correct"));
            //Assert.IsTrue(C("affirmative"));
        }

        private static bool C([NotNull] string input)
        {
            var extr = FuzzyParsing.BooleanChoice(input);

            Console.WriteLine($"{input} : {extr.Value} ({extr.Confidence})");

            return extr.Value;
        }

        [TestMethod]
        public void Money()
        {
            Assert.AreEqual(("Pound", 7.5M), M("you owe me £7.50"));
            Assert.AreEqual(("British pound", 9M), M("you owe me 9 gbp"));
            Assert.AreEqual(("Dollar", 10M), M("Oh i send you that $10 by bank transfer earlier btw"));

            Assert.AreEqual(null, M("you owe me 3 quid"));
        }

        private static (string, decimal)? M([NotNull] string input)
        {
            var extr = FuzzyParsing.CurrencyAndAmount(input);

            Console.WriteLine($"{input} : {extr.Amount} {extr.Currency}");

            if (!extr.IsValid)
                return null;

            return (extr.Currency, extr.Amount);
        }

        [TestMethod]
        public void DateTimeRange()
        {
            var r1 = DateTimeRecognizer.RecognizeDateTime("next week", "en-gb");
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(r1));

            var r2 = FuzzyParsing.MomentRange("next week");
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(r2));

            var r3 = FuzzyParsing.Moment("monday");
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(r3));
        }

        [TestMethod]
        public void JsonRecord()
        {
            var add = new Add(
                new MacroInvoke("ns", "name", new IAstNode[] { new ConstantValue(1) }),
                new Negate(new ConstantValue(2))
            );

            var j1 = JsonSerializer.Serialize(add);
            Console.WriteLine(j1);

            var j2 = JsonSerializer.Serialize(add.Reduce());
            Console.WriteLine(j2);
        }
    }
}
