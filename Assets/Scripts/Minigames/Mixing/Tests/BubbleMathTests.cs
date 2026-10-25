using NUnit.Framework;

namespace Brewing.Mixing.Tests
{
    public class BubbleMathTests
    {
        [Test]
        public void NoBubblesAtOrBelowTheStartDanger()
        {
            Assert.AreEqual(0, BubbleMath.TargetCount(0f, 0.05f, 40, 1f));
            Assert.AreEqual(0, BubbleMath.TargetCount(0.05f, 0.05f, 40, 1f));
        }

        [Test]
        public void AtLeastOneBubbleOncePastTheStartDanger()
        {
            Assert.AreEqual(1, BubbleMath.TargetCount(0.051f, 0.05f, 40, 1f));
        }

        [Test]
        public void FullDangerGivesTheMaximum()
        {
            Assert.AreEqual(40, BubbleMath.TargetCount(1f, 0.05f, 40, 1f));
            Assert.AreEqual(40, BubbleMath.TargetCount(2f, 0.05f, 40, 1f), "clamped");
        }

        [Test]
        public void CountRisesWithDanger()
        {
            int low = BubbleMath.TargetCount(0.2f, 0.05f, 40, 1f);
            int mid = BubbleMath.TargetCount(0.5f, 0.05f, 40, 1f);
            int high = BubbleMath.TargetCount(0.8f, 0.05f, 40, 1f);
            Assert.Less(low, mid);
            Assert.Less(mid, high);
        }

        [Test]
        public void ALargerPowerKeepsTheCountLowerForLonger()
        {
            Assert.Less(BubbleMath.TargetCount(0.5f, 0f, 40, 2f), BubbleMath.TargetCount(0.5f, 0f, 40, 1f));
        }

        [Test]
        public void NoMaximumMeansNoBubbles()
        {
            Assert.AreEqual(0, BubbleMath.TargetCount(1f, 0f, 0, 1f));
        }

        [Test]
        public void BubbleSwellsInHoldsAndPops()
        {
            BubbleMath.Evaluate(0f, out float scale, out float alpha);
            Assert.AreEqual(0f, scale, 1e-6f);
            Assert.AreEqual(0f, alpha, 1e-6f);

            BubbleMath.Evaluate(0.5f, out scale, out alpha);
            Assert.AreEqual(1f, scale, 1e-6f);
            Assert.AreEqual(1f, alpha, 1e-6f);

            BubbleMath.Evaluate(0.925f, out scale, out alpha);
            Assert.Greater(scale, 1f);
            Assert.AreEqual(0.5f, alpha, 1e-3f);

            BubbleMath.Evaluate(1f, out scale, out alpha);
            Assert.AreEqual(0f, alpha, 1e-6f);
        }
    }
}
