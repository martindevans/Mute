using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Moe.Extensions;

namespace Mute.Tests
{
    [TestClass]
    public class FriendlyId32Tests
    {
        [TestMethod]
        public void Roundtrip()
        {
            const uint a = 2323423;
            var str = a.MeaninglessString();
            Console.WriteLine(str);
            var b = FriendlyId32.Parse(str);
            Assert.IsNotNull(b);
            Assert.AreEqual(a, b.Value.Value);
        }

        [TestMethod]
        public void AllZero()
        {
            const uint a = 0;
            var str = a.MeaninglessString();
            Console.WriteLine(str);
            var b = FriendlyId32.Parse(str);
            Assert.IsNotNull(b);
            Assert.AreEqual(a, b.Value.Value);
        }

        [TestMethod]
        public void Parse()
        {
            var str = "lomryc-racnes";
            var fid = FriendlyId32.Parse(str);

            Console.WriteLine(str);
            Assert.IsTrue(fid.HasValue);
            Console.WriteLine(fid.Value.Value);
            Console.WriteLine(fid.ToString());
        }
    }
}
