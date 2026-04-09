using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Moe.Extensions;

namespace Mute.Tests.Extensions;

[TestClass]
public class IntExtensionsTests
{
    [TestMethod]
    public void IsPowerOfTwo_PowersOfTwo_AreTrue()
    {
        Assert.IsTrue(1.IsPowerOfTwo());
        Assert.IsTrue(2.IsPowerOfTwo());
        Assert.IsTrue(4.IsPowerOfTwo());
        Assert.IsTrue(1024.IsPowerOfTwo());
    }

    [TestMethod]
    public void IsPowerOfTwo_NonPowersOfTwo_AreFalse()
    {
        Assert.IsFalse(0.IsPowerOfTwo());
        Assert.IsFalse(3.IsPowerOfTwo());
        Assert.IsFalse(5.IsPowerOfTwo());
        Assert.IsFalse(6.IsPowerOfTwo());
        Assert.IsFalse((-1).IsPowerOfTwo());
        Assert.IsFalse((-2).IsPowerOfTwo());
    }

    [TestMethod]
    public void IsPowerOfTwo_IntMinValue_IsTrue()
    {
        // int.MinValue (-2147483648) cast to uint is 2^31, which has exactly one bit set
        Assert.IsTrue(int.MinValue.IsPowerOfTwo());
    }
}