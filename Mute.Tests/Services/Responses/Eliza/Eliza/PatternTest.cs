using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Services.Responses.Eliza.Eliza;

namespace Mute.Tests.Services.Responses.Eliza.Eliza
{
    [TestClass]
    public class PatternTest
    {
        [TestMethod]
        public void MatchString()
        {
            const string str = "abc123";

            var matches = new string[10];
            Assert.IsTrue(Patterns.Match(str, "*", matches));

            Assert.AreEqual(str, matches[0]);
        }

        [TestMethod]
        public void AmpersandIsIgnored()
        {
            const string str = "abc @123";

            var matches = new string[10];
            Assert.IsTrue(Patterns.Match(str, "*", matches));

            Assert.AreEqual(str, matches[0]);
        }

        [TestMethod]
        public void MatchStringWithAmpersand()
        {
            const string str = "abc @123";

            var matches = new string[10];
            Assert.IsTrue(Patterns.Match(str, "* @*", matches));

            Assert.AreEqual("abc", matches[0]);
            Assert.AreEqual("123", matches[1]);
        }

        [TestMethod]
        public void TildeIsIgnored()
        {
            const string str = "abc ~123";

            var matches = new string[10];
            Assert.IsTrue(Patterns.Match(str, "*", matches));

            Assert.AreEqual(str, matches[0]);
        }

        [TestMethod]
        public void MatchStringWithTilde()
        {
            const string str = "abc ~123";

            var matches = new string[10];
            Assert.IsTrue(Patterns.Match(str, "* ~*", matches));

            Assert.AreEqual("abc", matches[0]);
            Assert.AreEqual("123", matches[1]);
        }

        [TestMethod]
        public void MatchStringWithSpace()
        {
            const string str = "abc 123";

            var matches = new string[10];
            Assert.IsTrue(Patterns.Match(str, "* *", matches));

            Assert.AreEqual("abc", matches[0]);
            Assert.AreEqual("123", matches[1]);
        }

        [TestMethod]
        public void MatchNumber()
        {
            const string str = "123";

            var matches = new string[10];
            Assert.IsTrue(Patterns.Match(str, "#", matches));

            Assert.AreEqual("123", matches[0]);
        }

        [TestMethod]
        public void MatchNumbersWithSpaces()
        {
            const string str = "123 456";

            var matches = new string[10];
            Assert.IsTrue(Patterns.Match(str, "# #", matches));

            Assert.AreEqual("123", matches[0]);
            Assert.AreEqual("456", matches[1]);
        }

        [TestMethod]
        public void BrotherRegression()
        {
            const string str = "my brother is philip";

            var matches = new string[10];
            Assert.IsTrue(Patterns.Match(str, "*my* brother *", matches));

            Assert.AreEqual("", matches[0]);
            Assert.AreEqual("", matches[1]);
            Assert.AreEqual("is philip", matches[2]);
        }
    }
}
