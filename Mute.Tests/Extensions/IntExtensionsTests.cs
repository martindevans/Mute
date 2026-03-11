using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Moe.Extensions;

namespace Mute.Tests.Extensions
{
    [TestClass]
    public class IntExtensionsTests
    {
        [TestMethod]
        public void IsPowerOfTwo_One_IsTrue()
        {
            Assert.IsTrue(1.IsPowerOfTwo());
        }

        [TestMethod]
        public void IsPowerOfTwo_Two_IsTrue()
        {
            Assert.IsTrue(2.IsPowerOfTwo());
        }

        [TestMethod]
        public void IsPowerOfTwo_Four_IsTrue()
        {
            Assert.IsTrue(4.IsPowerOfTwo());
        }

        [TestMethod]
        public void IsPowerOfTwo_LargePowerOfTwo_IsTrue()
        {
            Assert.IsTrue(1024.IsPowerOfTwo());
        }

        [TestMethod]
        public void IsPowerOfTwo_Zero_IsFalse()
        {
            Assert.IsFalse(0.IsPowerOfTwo());
        }

        [TestMethod]
        public void IsPowerOfTwo_Three_IsFalse()
        {
            Assert.IsFalse(3.IsPowerOfTwo());
        }

        [TestMethod]
        public void IsPowerOfTwo_Five_IsFalse()
        {
            Assert.IsFalse(5.IsPowerOfTwo());
        }

        [TestMethod]
        public void IsPowerOfTwo_Six_IsFalse()
        {
            Assert.IsFalse(6.IsPowerOfTwo());
        }

        [TestMethod]
        public void IsPowerOfTwo_NegativeOne_IsFalse()
        {
            Assert.IsFalse((-1).IsPowerOfTwo());
        }

        [TestMethod]
        public void IsPowerOfTwo_NegativeTwo_IsFalse()
        {
            Assert.IsFalse((-2).IsPowerOfTwo());
        }

        [TestMethod]
        public void IsPowerOfTwo_IntMinValue_IsTrue()
        {
            // int.MinValue (-2147483648) cast to uint is 2^31, which has exactly one bit set
            Assert.IsTrue(int.MinValue.IsPowerOfTwo());
        }
    }
}
