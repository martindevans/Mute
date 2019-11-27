using System;
using System.Globalization;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Xml;
using Humanizer;
using Humanizer.DateTimeHumanizeStrategy;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Moe.Extensions;
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
    }
}
