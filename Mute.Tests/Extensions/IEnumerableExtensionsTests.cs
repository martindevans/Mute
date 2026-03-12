using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Moe.Extensions;

namespace Mute.Tests.Extensions
{
    [TestClass]
    public class IEnumerableExtensionsTests
    {
        [TestMethod]
        public void RandomItemSingleItemList()
        {
            var l = (IEnumerable<int>)[ 1 ];
            Assert.AreEqual(1, l.Random(new Random()));
        }

        [TestMethod]
        public void RandomItemFromList()
        {
            var l = (IEnumerable<int>)[ 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 ];

            var r = new Random();
            Assert.IsGreaterThan(5, Enumerable.Range(0, 1000).Select(_ => l.Random(r)).Distinct().Count());
        }

        [TestMethod]
        public void Random_NullInput_ReturnsDefault()
        {
            IEnumerable<int>? nullList = null;
            Assert.AreEqual(default(int), nullList.Random(new Random()));
        }

        [TestMethod]
        public void Random_NullInput_ReturnsDefaultForReferenceType()
        {
            IEnumerable<string>? nullList = null;
            Assert.IsNull(nullList.Random(new Random()));
        }

        [TestMethod]
        public void Random_EmptyList_ReturnsDefault()
        {
            var emptyList = Array.Empty<int>();
            Assert.AreEqual(default(int), emptyList.Random(new Random()));
        }

        [TestMethod]
        public void Random_EmptyList_ReturnsDefaultForReferenceType()
        {
            var emptyList = Array.Empty<string>();
            Assert.IsNull(emptyList.Random(new Random()));
        }
    }
}
