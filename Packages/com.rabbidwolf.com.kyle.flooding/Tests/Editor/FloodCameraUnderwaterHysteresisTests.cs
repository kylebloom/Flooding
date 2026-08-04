using NUnit.Framework;

namespace Kyle.Flooding.Tests
{
    public sealed class FloodCameraUnderwaterHysteresisTests
    {
        private const float Enter = -0.02f;
        private const float Exit = 0.02f;

        [Test]
        public void Dry_DoesNotEnter_UntilAtOrBelowEnterThreshold()
        {
            Assert.That(
                FloodCameraUnderwaterHysteresis.Evaluate(
                    false, true, 0f, Enter, Exit),
                Is.False);
            Assert.That(
                FloodCameraUnderwaterHysteresis.Evaluate(
                    false, true, -0.019f, Enter, Exit),
                Is.False);
            Assert.That(
                FloodCameraUnderwaterHysteresis.Evaluate(
                    false, true, Enter, Enter, Exit),
                Is.True);
            Assert.That(
                FloodCameraUnderwaterHysteresis.Evaluate(
                    false, true, -0.05f, Enter, Exit),
                Is.True);
        }

        [Test]
        public void Underwater_DoesNotExit_UntilAtOrAboveExitThreshold()
        {
            Assert.That(
                FloodCameraUnderwaterHysteresis.Evaluate(
                    true, true, 0f, Enter, Exit),
                Is.True);
            Assert.That(
                FloodCameraUnderwaterHysteresis.Evaluate(
                    true, true, 0.019f, Enter, Exit),
                Is.True);
            Assert.That(
                FloodCameraUnderwaterHysteresis.Evaluate(
                    true, true, Exit, Enter, Exit),
                Is.False);
            Assert.That(
                FloodCameraUnderwaterHysteresis.Evaluate(
                    true, true, 0.05f, Enter, Exit),
                Is.False);
        }

        [Test]
        public void OutsideVolume_AlwaysClearsUnderwater()
        {
            Assert.That(
                FloodCameraUnderwaterHysteresis.Evaluate(
                    true, false, -1f, Enter, Exit),
                Is.False);
            Assert.That(
                FloodCameraUnderwaterHysteresis.Evaluate(
                    false, false, -1f, Enter, Exit),
                Is.False);
        }

        [Test]
        public void OnSurfaceBand_PreservesLatchedState()
        {
            Assert.That(
                FloodCameraUnderwaterHysteresis.Evaluate(
                    false, true, 0f, Enter, Exit),
                Is.False);
            Assert.That(
                FloodCameraUnderwaterHysteresis.Evaluate(
                    true, true, 0f, Enter, Exit),
                Is.True);
        }
    }
}
