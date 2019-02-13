using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Moe.Extensions;

namespace Mute.Tests
{
    [TestClass]
    public class FriendlyId32Tests
    {
        [TestMethod]
        public void ParseInvalid()
        {
            Assert.IsNull(FriendlyId32.Parse("aaaryc-racnes"));
            Assert.IsNull(FriendlyId32.Parse("molaaa-racnes"));
            Assert.IsNull(FriendlyId32.Parse("molryc-aaanes"));
            Assert.IsNull(FriendlyId32.Parse("molryc-racaaa"));
            Assert.IsNull(FriendlyId32.Parse("molrycracten"));
            Assert.IsNull(FriendlyId32.Parse("hello"));
            Assert.IsNull(FriendlyId32.Parse("hello-world"));
            Assert.IsNull(FriendlyId32.Parse("molryc-world"));
        }

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

            Assert.IsTrue(fid.HasValue);
            Assert.AreEqual(str, fid.ToString());

            Console.WriteLine(str);
            Console.WriteLine(fid.Value.Value);
            Console.WriteLine(fid.ToString());
        }

        [TestMethod]
        public void First100()
        {
            for (uint i = 0; i < 100; i++)
            {
                Console.WriteLine(new FriendlyId32(i).ToString());
            }
        }
    }
}
