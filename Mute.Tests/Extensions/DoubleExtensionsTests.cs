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
        public void LogitToProbability_Double_BasicBehaviour()
        {
            Assert.AreEqual(0.5, 0.0.LogitToProbability(), Tolerance);
            Assert.IsTrue(1.0.LogitToProbability() > 0.5);
            Assert.IsTrue((-1.0).LogitToProbability() < 0.5);
            Assert.AreEqual(1.0, 100.0.LogitToProbability(), 1e-6);
            Assert.AreEqual(0.0, (-100.0).LogitToProbability(), 1e-6);
        }

        [TestMethod]
        public void LogitToProbability_Double_Fuzz()
        {
            const int iterations = 10_000;
            const double step = 200.0 / iterations; // covers -100 to +100

            for (var i = 0; i < iterations; i++)
            {
                var logit = -100.0 + i * step;
                var p = logit.LogitToProbability();

                Assert.IsTrue(p >= 0.0 && p <= 1.0, $"Probability {p} for logit {logit} is out of [0,1]");
                // sigmoid(-x) == 1 - sigmoid(x)
                Assert.AreEqual(p, 1.0 - (-logit).LogitToProbability(), Tolerance, $"Symmetry violated at logit {logit}");
            }
        }

        [TestMethod]
        public void LogitToProbability_Float_BasicBehaviour()
        {
            Assert.AreEqual(0.5f, 0.0f.LogitToProbability(), FloatTolerance);
            Assert.IsTrue(1.0f.LogitToProbability() > 0.5f);
            Assert.IsTrue((-1.0f).LogitToProbability() < 0.5f);
            Assert.AreEqual(1.0f, 100.0f.LogitToProbability(), 1e-6f);
            Assert.AreEqual(0.0f, (-100.0f).LogitToProbability(), 1e-6f);
        }

        [TestMethod]
        public void LogitToProbability_Float_Fuzz()
        {
            const int iterations = 10_000;
            const float step = 200.0f / iterations; // covers -100 to +100

            for (var i = 0; i < iterations; i++)
            {
                var logit = -100.0f + i * step;
                var p = logit.LogitToProbability();

                Assert.IsTrue(p >= 0.0f && p <= 1.0f, $"Probability {p} for logit {logit} is out of [0,1]");
                // sigmoid(-x) == 1 - sigmoid(x)
                Assert.AreEqual(p, 1.0f - (-logit).LogitToProbability(), FloatTolerance, $"Symmetry violated at logit {logit}");
            }
        }
    }
}
