using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Moe.Extensions;

namespace Mute.Tests.Extensions
{
    [TestClass]
    public class FriendlyId64Tests
    {
        [TestMethod]
        public void ParseInvalid()
        {
            Assert.IsNull(FriendlyId64.Parse("aaaryc-racnes"));
            Assert.IsNull(FriendlyId64.Parse("molaaa-racnes"));
            Assert.IsNull(FriendlyId64.Parse("molryc-aaanes"));
            Assert.IsNull(FriendlyId64.Parse("molryc-racaaa"));
            Assert.IsNull(FriendlyId64.Parse("molrycracten"));
            Assert.IsNull(FriendlyId64.Parse("hello"));
            Assert.IsNull(FriendlyId64.Parse("hello-world"));
            Assert.IsNull(FriendlyId64.Parse("molryc-world"));
        }

        [TestMethod]
        public void Roundtrip()
        {
            const ulong a = 232342389035467834;
            var str = a.MeaninglessString();
            Console.WriteLine(str);
            var b = FriendlyId64.Parse(str);
            Assert.IsNotNull(b);
            Assert.AreEqual(a, b.Value.Value);
        }

        [TestMethod]
        public void AllZero()
        {
            const ulong a = 0;
            var str = a.MeaninglessString();
            Console.WriteLine(str);
            var b = FriendlyId64.Parse(str);
            Assert.IsNotNull(b);
            Assert.AreEqual(a, b.Value.Value);
        }

        [TestMethod]
        public void Parse()
        {
            var str = "solser-datwed-widmut-dabten";
            var fid = FriendlyId64.Parse(str);

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
                Console.WriteLine(new FriendlyId64(i).ToString());
            }
        }
    }
}
