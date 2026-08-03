using NUnit.Framework;

namespace Kyle.Flooding.Tests
{
    public sealed class FloodFluidTolerancesTests
    {
        [Test]
        public void DensitiesMatch_AcceptsNearlyEqualFreshWaterValues()
        {
            Assert.That(
                FloodFluidTolerances.DensitiesMatch(1000d, 1000.0005d),
                Is.True);
        }

        [Test]
        public void DensitiesMatch_RejectsMateriallyDifferentValues()
        {
            Assert.That(
                FloodFluidTolerances.DensitiesMatch(1000d, 1025d),
                Is.False);
        }

        [Test]
        public void NearlyEqual_UsesAbsoluteToleranceNearZero()
        {
            Assert.That(
                FloodFluidTolerances.NearlyEqual(
                    0d,
                    FloodFluidTolerances.DensityAbsolute * 0.5d,
                    FloodFluidTolerances.DensityAbsolute,
                    FloodFluidTolerances.DensityRelative),
                Is.True);
        }
    }
}
