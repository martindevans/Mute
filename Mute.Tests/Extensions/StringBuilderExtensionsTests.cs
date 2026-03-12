using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Moe.Extensions;

namespace Mute.Tests.Extensions
{
    [TestClass]
    public class StringBuilderExtensionsTests
    {
        [TestMethod]
        public void AppendLine_AppendsSpanAndNewline()
        {
            var builder = new StringBuilder();
            builder.AppendLine("hello".AsSpan());

            Assert.AreEqual("hello" + Environment.NewLine, builder.ToString());
        }

        [TestMethod]
        public void AppendLine_EmptySpan_AppendsOnlyNewline()
        {
            var builder = new StringBuilder();
            builder.AppendLine(ReadOnlySpan<char>.Empty);

            Assert.AreEqual(Environment.NewLine, builder.ToString());
        }

        [TestMethod]
        public void AppendLine_MultipleChainedCalls_ProducesCorrectResult()
        {
            var builder = new StringBuilder();
            builder.AppendLine("line1".AsSpan())
                   .AppendLine("line2".AsSpan())
                   .AppendLine("line3".AsSpan());

            var expected = "line1" + Environment.NewLine +
                           "line2" + Environment.NewLine +
                           "line3" + Environment.NewLine;

            Assert.AreEqual(expected, builder.ToString());
        }
    }
}
