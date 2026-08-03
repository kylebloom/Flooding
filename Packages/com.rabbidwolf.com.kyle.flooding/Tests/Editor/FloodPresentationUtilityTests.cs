using NUnit.Framework;

namespace Kyle.Flooding.Tests
{
    public sealed class FloodPresentationUtilityTests
    {
        [Test]
        public void FlowIntensity_ReturnsZeroForIdleFlow()
        {
            Assert.That(
                FloodPresentationUtility.FlowIntensity(0d, 0.25f, 2f),
                Is.Zero);
            Assert.That(
                FloodPresentationUtility.IsFlowing(FloodPresentationUtility.IdleFlowRate),
                Is.False);
        }

        [Test]
        public void FlowIntensity_IncreasesWithFlowRate()
        {
            var low = FloodPresentationUtility.FlowIntensity(0.1d, 0.25f, 2f);
            var mid = FloodPresentationUtility.FlowIntensity(1d, 0.25f, 2f);
            var high = FloodPresentationUtility.FlowIntensity(3d, 0.25f, 2f);

            Assert.That(low, Is.GreaterThan(0f).And.LessThan(mid));
            Assert.That(mid, Is.LessThan(high));
            Assert.That(high, Is.EqualTo(1f));
        }

        [Test]
        public void FillIntensity_ClampsToUnitInterval()
        {
            Assert.That(FloodPresentationUtility.FillIntensity(-1d), Is.Zero);
            Assert.That(FloodPresentationUtility.FillIntensity(0.5d), Is.EqualTo(0.5f));
            Assert.That(FloodPresentationUtility.FillIntensity(2d), Is.EqualTo(1f));
        }
    }
}
