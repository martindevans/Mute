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
    }
}
