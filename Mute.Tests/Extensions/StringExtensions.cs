using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Moe.Extensions;

namespace Mute.Tests.Extensions
{
    [TestClass]
    public class StringExtensions
    {
        [TestMethod]
        public void Levenshtein_Similar()
        {
            Assert.AreEqual(1, (int)"aaa".Levenshtein("aab"));
        }

        [TestMethod]
        public void Levenshtein_Different()
        {
            Assert.AreEqual(3, (int)"aaa".Levenshtein("bbb"));
        }

        [TestMethod]
        public void Levenshtein_Similar_ExceptLength()
        {
            Assert.AreEqual(1, (int)"aaa".Levenshtein("aaaa"));
        }

        [TestMethod]
        public void Levenshtein_Different_WithLength()
        {
            Assert.AreEqual(4, (int)"aaa".Levenshtein("bbbb"));
        }

        [TestMethod]
        public void SplitSpan()
        {
            const string str = "a b c";
            var split = str.SplitSpan(' ').ToArray();

            Assert.AreEqual(3, split.Length);
            Assert.AreEqual("a", split[0].ToString());
            Assert.AreEqual("b", split[1].ToString());
            Assert.AreEqual("c", split[2].ToString());
        }

        [TestMethod]
        public void SplitSpanWithEmpty()
        {
            const string str = "a b c   d";

            var split = str.SplitSpan(' ').ToArray();

            Assert.AreEqual(6, split.Length);
            Assert.AreEqual("a", split[0].ToString());
            Assert.AreEqual("b", split[1].ToString());
            Assert.AreEqual("c", split[2].ToString());
            Assert.AreEqual("", split[3].ToString());
            Assert.AreEqual("", split[4].ToString());
            Assert.AreEqual("d", split[5].ToString());
        }

        [TestMethod]
        public void SplitSpanWithEmptyRemoved()
        {
            const string str = "a b c   d";

            var split = str.SplitSpan(' ', System.StringSplitOptions.RemoveEmptyEntries).ToArray();

            Assert.AreEqual(4, split.Length);
            Assert.AreEqual("a", split[0].ToString());
            Assert.AreEqual("b", split[1].ToString());
            Assert.AreEqual("c", split[2].ToString());
            Assert.AreEqual("d", split[3].ToString());
        }
    }
}
