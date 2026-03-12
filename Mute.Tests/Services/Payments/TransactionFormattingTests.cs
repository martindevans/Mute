using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Moe.Discord.Modules.Payment;

namespace Mute.Tests.Services.Payments
{
    [TestClass]
    public class TransactionFormattingTests
    {
        [TestMethod]
        public void FormatCurrency_GBP_UsesPoundSymbol()
        {
            var result = TransactionFormatting.FormatCurrency(10, "GBP");
            Assert.AreEqual("£10", result);
        }

        [TestMethod]
        public void FormatCurrency_USD_UsesDollarSymbol()
        {
            var result = TransactionFormatting.FormatCurrency(5, "USD");
            Assert.AreEqual("$5", result);
        }

        [TestMethod]
        public void FormatCurrency_EUR_UsesEuroSymbol()
        {
            var result = TransactionFormatting.FormatCurrency(7, "EUR");
            Assert.AreEqual("€7", result);
        }

        [TestMethod]
        public void FormatCurrency_UnknownUnit_UsesFallbackFormat()
        {
            var result = TransactionFormatting.FormatCurrency(3, "XYZ");
            Assert.AreEqual("3(XYZ)", result);
        }

        [TestMethod]
        public void FormatCurrency_CaseInsensitive_LowercaseGbp()
        {
            var lower = TransactionFormatting.FormatCurrency(10, "gbp");
            var upper = TransactionFormatting.FormatCurrency(10, "GBP");
            Assert.AreEqual(lower, upper);
        }

        [TestMethod]
        public void FormatCurrency_DecimalAmount()
        {
            var result = TransactionFormatting.FormatCurrency(10.50m, "GBP");
            Assert.AreEqual("£10.50", result);
        }
    }
}
