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
            var l = (IEnumerable<int>)new[] { 1 };
            Assert.AreEqual(1, l.Random(new Random()));
        }

        [TestMethod]
        public void RandomItemFromList()
        {
            var l = (IEnumerable<int>)new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            var r = new Random();
            Assert.IsTrue(Enumerable.Range(0, 1000).Select(_ => l.Random(r)).Distinct().Count() > 5);
        }
    }
}
