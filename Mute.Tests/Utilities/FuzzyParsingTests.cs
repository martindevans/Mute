using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Moe.Utilities;

namespace Mute.Tests.Utilities
{
    [TestClass]
    public class FuzzyParsingTests
    {
        [TestMethod]
        public void ParseSimpleTime()
        {
            var time = FuzzyParsing.Moment("15:35 tomorrow");

            Assert.IsTrue(time.IsValid);

            var tomorrow = DateTime.UtcNow.AddDays(1);
            var expected = new DateTime(tomorrow.Year, tomorrow.Month, tomorrow.Day, 15, 35, 0);

            Assert.AreEqual(expected, time.Value);
        }

        [TestMethod]
        public void ParseSimpleTimeWithTz()
        {
            var time = FuzzyParsing.Moment("15:35 BST tomorrow");

            Assert.IsTrue(time.IsValid);

            var tomorrow = DateTime.UtcNow.AddDays(1);
            var expected = new DateTime(tomorrow.Year, tomorrow.Month, tomorrow.Day, 14, 35, 0);

            Assert.AreEqual(expected, time.Value);
        }

        [TestMethod]
        public void ParseComplexTime()
        {
            var time = FuzzyParsing.Moment("Midday tomorrow");

            Assert.IsTrue(time.IsValid);

            var tomorrow = DateTime.UtcNow.AddDays(1);
            var expected = new DateTime(tomorrow.Year, tomorrow.Month, tomorrow.Day, 12, 00, 0);

            Assert.AreEqual(expected, time.Value);
        }
    }
}
