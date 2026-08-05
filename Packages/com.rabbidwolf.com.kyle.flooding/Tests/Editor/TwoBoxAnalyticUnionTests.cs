using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Kyle.Flooding.Tests
{
    public sealed class TwoBoxAnalyticUnionTests
    {
        [Test]
        public void OverlappingBoxes_CountCapacityOnce()
        {
            using var fixture = new TwoBoxFixture(
                volumeAPosition: new Vector3(-0.5f, 0f, 0f),
                volumeBPosition: new Vector3(0.5f, 0f, 0f),
                width: 2f,
                length: 2f,
                height: 2f);

            Assert.That(fixture.Geometry.Capacity, Is.EqualTo(12d).Within(1e-6d));
        }

        [Test]
        public void NonOverlappingTouchingBoxes_SumCapacity()
        {
            using var fixture = new TwoBoxFixture(
                volumeAPosition: new Vector3(-1f, 0f, 0f),
                volumeBPosition: new Vector3(1f, 0f, 0f),
                width: 2f,
                length: 2f,
                height: 2f);

            // Face-touching at x=0: no volume overlap.
            Assert.That(fixture.Geometry.Capacity, Is.EqualTo(16d).Within(1e-6d));
        }

        [Test]
        public void PartialFill_DoesNotDoubleCountOverlap()
        {
            using var fixture = new TwoBoxFixture(
                volumeAPosition: new Vector3(-0.5f, 0f, 0f),
                volumeBPosition: new Vector3(0.5f, 0f, 0f),
                width: 2f,
                length: 2f,
                height: 2f);

            var plane = new Plane(Vector3.up, new Vector3(0f, 1f, 0f));
            var filled = fixture.Geometry.CalculateSubmergedVolume(plane);

            // Half height of union capacity 12 → 6.
            Assert.That(filled, Is.EqualTo(6d).Within(1e-5d));
        }

        [Test]
        public void SurfaceSolver_FindsOnePlaneForUnionVolume()
        {
            using var fixture = new TwoBoxFixture(
                volumeAPosition: new Vector3(-0.5f, 0f, 0f),
                volumeBPosition: new Vector3(0.5f, 0f, 0f),
                width: 2f,
                length: 2f,
                height: 2f);

            var solution = FloodSurfaceSolver.Solve(
                fixture.Geometry,
                Vector3.up,
                targetVolume: 6d);

            Assert.That(
                solution.Submersion.Volume,
                Is.EqualTo(6d).Within(1e-5d));
            Assert.That(
                solution.LocalSurfacePlane.normal.y,
                Is.GreaterThan(0.99f));
        }

        [Test]
        public void DisconnectedBoxes_FailValidation()
        {
            var region = new GameObject("Region");
            var volumeA = CreateRectangularMember(
                region.transform,
                new Vector3(-3f, 0f, 0f),
                2f,
                2f,
                2f);
            var volumeB = CreateRectangularMember(
                region.transform,
                new Vector3(3f, 0f, 0f),
                2f,
                2f,
                2f);

            var strategy = new TwoBoxAnalyticUnionStrategy();
            var built = strategy.TryBuild(
                region.transform,
                new List<FloodVolume> { volumeA, volumeB },
                out _,
                out var message);

            Assert.That(built, Is.False);
            Assert.That(message, Does.Contain("disconnected"));

            Object.DestroyImmediate(region);
        }

        [Test]
        public void ContainsLocalPoint_UsesUnion()
        {
            using var fixture = new TwoBoxFixture(
                volumeAPosition: new Vector3(-0.5f, 0f, 0f),
                volumeBPosition: new Vector3(0.5f, 0f, 0f),
                width: 2f,
                length: 2f,
                height: 2f);

            Assert.That(
                fixture.Geometry.ContainsLocalPoint(new Vector3(-1f, 0.5f, 0f)),
                Is.True);
            Assert.That(
                fixture.Geometry.ContainsLocalPoint(new Vector3(1f, 0.5f, 0f)),
                Is.True);
            Assert.That(
                fixture.Geometry.ContainsLocalPoint(new Vector3(0f, 0.5f, 0f)),
                Is.True);
            Assert.That(
                fixture.Geometry.ContainsLocalPoint(new Vector3(3f, 0.5f, 0f)),
                Is.False);
        }

        private static FloodVolume CreateRectangularMember(
            Transform region,
            Vector3 localPosition,
            float width,
            float length,
            float height)
        {
            var go = new GameObject("Member");
            go.transform.SetParent(region, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            var volume = go.AddComponent<FloodVolume>();
            volume.ConfigureRectangularGeometry(width, length, height);
            return volume;
        }

        private sealed class TwoBoxFixture : System.IDisposable
        {
            private readonly GameObject regionRoot;

            public TwoBoxFixture(
                Vector3 volumeAPosition,
                Vector3 volumeBPosition,
                float width,
                float length,
                float height)
            {
                regionRoot = new GameObject("Region");
                var volumeA = CreateRectangularMember(
                    regionRoot.transform,
                    volumeAPosition,
                    width,
                    length,
                    height);
                var volumeB = CreateRectangularMember(
                    regionRoot.transform,
                    volumeBPosition,
                    width,
                    length,
                    height);

                var strategy = new TwoBoxAnalyticUnionStrategy();
                var built = strategy.TryBuild(
                    regionRoot.transform,
                    new List<FloodVolume> { volumeA, volumeB },
                    out var geometry,
                    out var message);

                Assert.That(built, Is.True, message);
                Geometry = geometry;
            }

            public IFloodVolumeGeometry Geometry { get; }

            public void Dispose()
            {
                Object.DestroyImmediate(regionRoot);
            }
        }
    }
}
