using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Moe.Extensions;

namespace Mute.Tests.Extensions
{
    [TestClass]
    public class IReadonlyListExtensionsTests
    {
        [TestMethod]
        public void RandomItemSingleItemList()
        {
            var l = new[] { 1 };
            Assert.AreEqual(1, l.Random(new Random()));
        }

        [TestMethod]
        public void RandomItemFromList()
        {
            var l = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            var r = new Random();
            Assert.IsGreaterThan(5, Enumerable.Range(0, 1000).Select(_ => l.Random(r)).Distinct().Count());
        }

        [TestMethod]
        public void MinimumCommonWhitespacePrefixEmptyList()
        {
            var lines = Array.Empty<string>();
            Assert.AreEqual(0, lines.MinimumCommonWhitespacePrefix());
        }

        [TestMethod]
        public void MinimumCommonWhitespacePrefixAllBlankLines()
        {
            var lines = new[] { "", "   ", "\t" };
            Assert.AreEqual(0, lines.MinimumCommonWhitespacePrefix());
        }

        [TestMethod]
        public void MinimumCommonWhitespacePrefixNoPrefix()
        {
            var lines = new[] { "hello", "world" };
            Assert.AreEqual(0, lines.MinimumCommonWhitespacePrefix());
        }

        [TestMethod]
        public void MinimumCommonWhitespacePrefixSingleLineWithPrefix()
        {
            var lines = new[] { "   hello" };
            Assert.AreEqual(3, lines.MinimumCommonWhitespacePrefix());
        }

        [TestMethod]
        public void MinimumCommonWhitespacePrefixEqualPrefix()
        {
            var lines = new[] { "  foo", "  bar", "  baz" };
            Assert.AreEqual(2, lines.MinimumCommonWhitespacePrefix());
        }

        [TestMethod]
        public void MinimumCommonWhitespacePrefixVaryingPrefix()
        {
            var lines = new[] { "    foo", "  bar", "      baz" };
            Assert.AreEqual(2, lines.MinimumCommonWhitespacePrefix());
        }

        [TestMethod]
        public void MinimumCommonWhitespacePrefixIgnoresBlankLines()
        {
            var lines = new[] { "   foo", "", "   bar" };
            Assert.AreEqual(3, lines.MinimumCommonWhitespacePrefix());
        }

        [TestMethod]
        public void MinimumCommonWhitespacePrefixMixedBlankAndNonBlank()
        {
            var lines = new[] { "    foo", "  ", "  bar" };
            Assert.AreEqual(2, lines.MinimumCommonWhitespacePrefix());
        }
    }
}
