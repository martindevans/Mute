using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Moe.Extensions;

namespace Mute.Tests.Extensions
{
    [TestClass]
    public class RandomExtensionsTests
    {
        [TestMethod]
        public void NextSingle_ReturnsValueWithinRange()
        {
            var rng = new Random(42);
            var value = rng.NextSingle(1.0f, 5.0f);

            Assert.IsTrue(value >= 1.0f && value < 5.0f, $"Expected value in [1, 5) but got {value}");
        }

        [TestMethod]
        public void NextSingle_ManyCallsAllWithinRange()
        {
            var rng = new Random(0);
            const float min = -10.0f;
            const float max = 10.0f;

            for (var i = 0; i < 10000; i++)
            {
                var value = rng.NextSingle(min, max);
                Assert.IsTrue(value >= min && value < max, $"Value {value} was outside [{min}, {max})");
            }
        }

        [TestMethod]
        public void NextSingle_MinEqualsMax_ReturnsMin()
        {
            var rng = new Random(1);
            var value = rng.NextSingle(3.0f, 3.0f);

            Assert.AreEqual(3.0f, value);
        }

        [TestMethod]
        public void NextSingle_ThrowsWhenMinGreaterThanMax()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var rng = new Random();
                rng.NextSingle(5.0f, 1.0f);
            });
        }

        [TestMethod]
        public void NextSingle_ProducesVariedValues()
        {
            var rng = new Random(99);
            var values = Enumerable.Range(0, 1000).Select(_ => rng.NextSingle(0.0f, 1.0f)).Distinct().Count();

            Assert.IsGreaterThan(500, values);
        }
    }
}
