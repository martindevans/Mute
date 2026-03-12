using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Moe.Extensions;

namespace Mute.Tests.Extensions
{
    [TestClass]
    public class DoubleExtensionsTests
    {
        private const double Tolerance = 1e-9;
        private const float FloatTolerance = 1e-6f;

        [TestMethod]
        public void LogitToProbability_Double_ZeroLogit_ReturnsHalf()
        {
            Assert.AreEqual(0.5, 0.0.LogitToProbability(), Tolerance);
        }

        [TestMethod]
        public void LogitToProbability_Double_PositiveLogit_ReturnsAboveHalf()
        {
            Assert.IsTrue(1.0.LogitToProbability() > 0.5);
            Assert.IsTrue(10.0.LogitToProbability() > 0.5);
        }

        [TestMethod]
        public void LogitToProbability_Double_NegativeLogit_ReturnsBelowHalf()
        {
            Assert.IsTrue((-1.0).LogitToProbability() < 0.5);
            Assert.IsTrue((-10.0).LogitToProbability() < 0.5);
        }

        [TestMethod]
        public void LogitToProbability_Double_LargePositiveLogit_ReturnsCloseToOne()
        {
            Assert.AreEqual(1.0, 100.0.LogitToProbability(), 1e-6);
        }

        [TestMethod]
        public void LogitToProbability_Double_LargeNegativeLogit_ReturnsCloseToZero()
        {
            Assert.AreEqual(0.0, (-100.0).LogitToProbability(), 1e-6);
        }

        [TestMethod]
        public void LogitToProbability_Double_ResultIsAlwaysBetweenZeroAndOne()
        {
            foreach (var logit in new[] { -1000.0, -1.0, 0.0, 1.0, 1000.0 })
            {
                var p = logit.LogitToProbability();
                Assert.IsTrue(p >= 0.0 && p <= 1.0, $"Probability {p} for logit {logit} is out of range");
            }
        }

        [TestMethod]
        public void LogitToProbability_Double_SymmetryAroundZero()
        {
            // sigmoid(-x) == 1 - sigmoid(x)
            Assert.AreEqual(1.0.LogitToProbability(), 1.0 - (-1.0).LogitToProbability(), Tolerance);
            Assert.AreEqual(5.0.LogitToProbability(), 1.0 - (-5.0).LogitToProbability(), Tolerance);
        }

        [TestMethod]
        public void LogitToProbability_Float_ZeroLogit_ReturnsHalf()
        {
            Assert.AreEqual(0.5f, 0.0f.LogitToProbability(), FloatTolerance);
        }

        [TestMethod]
        public void LogitToProbability_Float_PositiveLogit_ReturnsAboveHalf()
        {
            Assert.IsTrue(1.0f.LogitToProbability() > 0.5f);
            Assert.IsTrue(10.0f.LogitToProbability() > 0.5f);
        }

        [TestMethod]
        public void LogitToProbability_Float_NegativeLogit_ReturnsBelowHalf()
        {
            Assert.IsTrue((-1.0f).LogitToProbability() < 0.5f);
            Assert.IsTrue((-10.0f).LogitToProbability() < 0.5f);
        }

        [TestMethod]
        public void LogitToProbability_Float_LargePositiveLogit_ReturnsCloseToOne()
        {
            Assert.AreEqual(1.0f, 100.0f.LogitToProbability(), 1e-6f);
        }

        [TestMethod]
        public void LogitToProbability_Float_LargeNegativeLogit_ReturnsCloseToZero()
        {
            Assert.AreEqual(0.0f, (-100.0f).LogitToProbability(), 1e-6f);
        }

        [TestMethod]
        public void LogitToProbability_Float_ResultIsAlwaysBetweenZeroAndOne()
        {
            foreach (var logit in new[] { -1000.0f, -1.0f, 0.0f, 1.0f, 1000.0f })
            {
                var p = logit.LogitToProbability();
                Assert.IsTrue(p >= 0.0f && p <= 1.0f, $"Probability {p} for logit {logit} is out of range");
            }
        }

        [TestMethod]
        public void LogitToProbability_Float_SymmetryAroundZero()
        {
            // sigmoid(-x) == 1 - sigmoid(x)
            Assert.AreEqual(1.0f.LogitToProbability(), 1.0f - (-1.0f).LogitToProbability(), FloatTolerance);
            Assert.AreEqual(5.0f.LogitToProbability(), 1.0f - (-5.0f).LogitToProbability(), FloatTolerance);
        }
    }
}
