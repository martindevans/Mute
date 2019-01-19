using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mute.Tests.Sigil
{
    [TestClass]
    public class SigilTests
    {
        [TestMethod]
        public void SigilFromByte()
        {
            var s = new Moe.Sigil.Sigil((byte)17);
            var str = s.ToSvg().ToString();
            Assert.IsNotNull(str);
            Console.WriteLine(str);
        }

        [TestMethod]
        public void SigilFromUShort()
        {
            var s = new Moe.Sigil.Sigil((ushort)34517);
            var str = s.ToSvg().ToString();
            Assert.IsNotNull(str);
            Console.WriteLine(str);
        }

        [TestMethod]
        public void SigilFromUInt()
        {
            var s = new Moe.Sigil.Sigil((uint)568524564);
            var str = s.ToSvg().ToString();
            Assert.IsNotNull(str);
            Console.WriteLine(str);
        }

        [TestMethod]
        public void SigilFromULong()
        {
            var s = new Moe.Sigil.Sigil((ulong)568556845665424564);
            var str = s.ToSvg().ToString();
            Assert.IsNotNull(str);
            Console.WriteLine(str);
        }
    }
}
