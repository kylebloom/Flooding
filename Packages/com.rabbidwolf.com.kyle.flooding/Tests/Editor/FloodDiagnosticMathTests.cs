using NUnit.Framework;
using UnityEngine;

namespace Kyle.Flooding.Tests
{
    public sealed class FloodDiagnosticMathTests
    {
        [Test]
        public void CombineWaterMass_UsesDeterministicMassWeightedCenter()
        {
            FloodState[] states =
            {
                CreateState(100d, new Vector3(-2f, 1f, 0f)),
                CreateState(300d, new Vector3(2f, 3f, 4f)),
            };

            var result = FloodDiagnosticMath.CombineWaterMass(
                states,
                new Vector3(99f, 99f, 99f));

            Assert.That(result.Mass, Is.EqualTo(400d));
            Assert.That(
                result.CenterOfMassWorld,
                Is.EqualTo(new Vector3(1f, 2.5f, 3f)));
        }

        [Test]
        public void CombineWaterMass_EmptyStatesUseDiagnosticOrigin()
        {
            var origin = new Vector3(4f, -2f, 8f);

            var result = FloodDiagnosticMath.CombineWaterMass(
                new[] { CreateState(0d, new Vector3(50f, 0f, 0f)) },
                origin);

            Assert.That(result.Mass, Is.Zero);
            Assert.That(result.CenterOfMassWorld, Is.EqualTo(origin));
        }

        [Test]
        public void ResolveFlowDirection_PrefersAppliedRate()
        {
            var direction = FloodDiagnosticMath.ResolveFlowDirection(
                new Vector3(0f, 0f, 2f),
                requestedFlowRate: 5d,
                appliedFlowRate: -1d);

            Assert.That(direction, Is.EqualTo(Vector3.back));
        }

        [Test]
        public void ResolveFlowDirection_UsesRequestedRateWhenNothingApplied()
        {
            var direction = FloodDiagnosticMath.ResolveFlowDirection(
                Vector3.right,
                requestedFlowRate: 2d,
                appliedFlowRate: 0d);

            Assert.That(direction, Is.EqualTo(Vector3.right));
        }

        private static FloodState CreateState(
            double waterMass,
            Vector3 centerOfMassWorld)
        {
            return new FloodState(
                volume: waterMass / 1000d,
                capacity: 10d,
                height: 1d,
                fillPercentage: 0.1d,
                isEmpty: waterMass <= 0d,
                isFull: false,
                surfacePlane: new Plane(Vector3.up, Vector3.zero),
                waterMass: waterMass,
                waterCenterOfMassWorld: centerOfMassWorld);
        }
    }
}
