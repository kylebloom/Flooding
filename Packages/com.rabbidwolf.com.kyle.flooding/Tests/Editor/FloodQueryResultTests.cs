using NUnit.Framework;
using UnityEngine;

namespace Kyle.Flooding.Tests
{
    public sealed class FloodQueryResultTests
    {
        [Test]
        public void SurfaceSignedDistance_PositiveMeansAboveSurface()
        {
            var result = CreateResult(
                isSubmerged: false,
                submersionDepthMeters: 0f,
                surfaceSignedDistanceMeters: 0.4f);

            Assert.That(result.SurfaceSignedDistanceMeters, Is.GreaterThan(0f));
            Assert.That(result.SubmersionDepthMeters, Is.Zero);
            Assert.That(result.IsSubmerged, Is.False);
        }

        [Test]
        public void SurfaceSignedDistance_ZeroMeansOnSurface()
        {
            var result = CreateResult(
                isSubmerged: false,
                submersionDepthMeters: 0f,
                surfaceSignedDistanceMeters: 0f);

            Assert.That(result.SurfaceSignedDistanceMeters, Is.Zero);
            Assert.That(result.SubmersionDepthMeters, Is.Zero);
        }

        [Test]
        public void SurfaceSignedDistance_NegativeMeansBelowSurface()
        {
            var result = CreateResult(
                isSubmerged: true,
                submersionDepthMeters: 0.25f,
                surfaceSignedDistanceMeters: -0.25f);

            Assert.That(result.SurfaceSignedDistanceMeters, Is.LessThan(0f));
            Assert.That(
                result.SubmersionDepthMeters,
                Is.EqualTo(-result.SurfaceSignedDistanceMeters)
                    .Within(0.0001f));
        }

        [Test]
        public void SubmersionDepth_RemainsZeroWhenBelowPlaneButNotSubmerged()
        {
            // Outside the compartment but geometrically below the infinite plane.
            var result = CreateResult(
                isInsideVolume: false,
                isSubmerged: false,
                submersionDepthMeters: 0f,
                surfaceSignedDistanceMeters: -0.3f);

            Assert.That(result.IsInsideVolume, Is.False);
            Assert.That(result.IsSubmerged, Is.False);
            Assert.That(result.SubmersionDepthMeters, Is.Zero);
            Assert.That(
                result.SurfaceSignedDistanceMeters,
                Is.EqualTo(-0.3f).Within(0.0001f));
        }

        [Test]
        public void Equals_IncludesSurfaceSignedDistance()
        {
            var left = CreateResult(
                isSubmerged: true,
                submersionDepthMeters: 0.2f,
                surfaceSignedDistanceMeters: -0.2f);
            var right = CreateResult(
                isSubmerged: true,
                submersionDepthMeters: 0.2f,
                surfaceSignedDistanceMeters: -0.1f);

            Assert.That(left == right, Is.False);
            Assert.That(left.Equals(right), Is.False);
            Assert.That(left.GetHashCode(), Is.Not.EqualTo(right.GetHashCode()));
        }

        private static FloodQueryResult CreateResult(
            bool isSubmerged,
            float submersionDepthMeters,
            float surfaceSignedDistanceMeters,
            bool isInsideVolume = true)
        {
            return new FloodQueryResult(
                isInsideVolume,
                isSubmerged,
                submersionDepthMeters,
                Vector3.zero,
                Vector3.up,
                surfaceSignedDistanceMeters);
        }
    }
}
