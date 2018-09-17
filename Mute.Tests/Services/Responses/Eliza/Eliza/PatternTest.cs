using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Services.Responses.Eliza.Engine;

namespace Mute.Tests.Services.Responses.Eliza.Eliza
{
    [TestClass]
    public class PatternTest
    {
        private readonly List<List<string>> _synonyms = new List<List<string>> {
            new List<string>{ "a", "aa", "aaa" },
            new List<string>{ "b", "bb", "bbb" },
            new List<string>{ "c", "cc", "ccc" },
        };

        [TestMethod]
        public void MatchString()
        {
            const string str = "abc123";

            var matches = Patterns.Match(str, "*", _synonyms);

            Assert.IsNotNull(matches);
            Assert.AreEqual(str, matches[0]);
        }

        [TestMethod]
        public void AtIsIgnored()
        {
            const string str = "abc @123";

            var matches = Patterns.Match(str, "*", _synonyms);

            Assert.IsNotNull(matches);
            Assert.AreEqual(str, matches[0]);
        }

        [TestMethod]
        public void TildeIsIgnored()
        {
            const string str = "abc ~123";

            var matches = Patterns.Match(str, "*", _synonyms);

            Assert.IsNotNull(matches);
            Assert.AreEqual(str, matches[0]);
        }

        [TestMethod]
        public void MatchStringWithTilde()
        {
            const string str = "abc ~123";

            var matches = Patterns.Match(str, "* ~*", _synonyms);

            Assert.IsNotNull(matches);
            Assert.AreEqual("abc", matches[0]);
            Assert.AreEqual("123", matches[1]);
        }

        [TestMethod]
        public void MatchStringWithSpace()
        {
            const string str = "abc 123";

            var matches = Patterns.Match(str, "* *", _synonyms);

            Assert.IsNotNull(matches);
            Assert.AreEqual("abc", matches[0]);
            Assert.AreEqual("123", matches[1]);
        }

        [TestMethod]
        public void MatchNumber()
        {
            const string str = "123";

            var matches = Patterns.Match(str, "#", _synonyms);

            Assert.IsNotNull(matches);
            Assert.AreEqual("123", matches[0]);
        }

        [TestMethod]
        public void MatchNumbersWithSpaces()
        {
            const string str = "123 456";

            var matches = Patterns.Match(str, "# #", _synonyms);

            Assert.IsNotNull(matches);
            Assert.AreEqual("123", matches[0]);
            Assert.AreEqual("456", matches[1]);
        }

        [TestMethod]
        public void BrotherRegression()
        {
            const string str = "my brother is philip";

            var matches = Patterns.Match(str, "*my* brother *", _synonyms);

            Assert.IsNotNull(matches);
            Assert.AreEqual("", matches[0]);
            Assert.AreEqual("", matches[1]);
            Assert.AreEqual("is philip", matches[2]);
        }

        [TestMethod]
        public void MatchWithSynonyms()
        {
            const string str = "hello aa bb ccc world";

            var matches = Patterns.Match(str, "* @a @b @c *", _synonyms);

            Assert.IsNotNull(matches);
            Assert.AreEqual("hello", matches[0]);
            Assert.AreEqual("aa", matches[1]);
            Assert.AreEqual("bb", matches[2]);
            Assert.AreEqual("ccc", matches[3]);
            Assert.AreEqual("world", matches[4]);
        }
    }
}
