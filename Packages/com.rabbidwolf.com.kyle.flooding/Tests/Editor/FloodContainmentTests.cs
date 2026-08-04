using NUnit.Framework;
using UnityEngine;

namespace Kyle.Flooding.Tests
{
    public sealed class FloodContainmentTests
    {
        [Test]
        public void RectangularPrism_ReportsExactContainment()
        {
            var geometry = new RectangularPrismFloodGeometry(4d, 2d, 3d);

            Assert.That(
                geometry.ContainmentPrecision,
                Is.EqualTo(FloodContainmentPrecision.Exact));
            Assert.That(
                geometry.ContainsLocalPoint(new Vector3(0f, 1.5f, 0f)),
                Is.True);
            Assert.That(
                geometry.ContainsLocalPoint(new Vector3(1.9f, 0f, 0.9f)),
                Is.True);
            Assert.That(
                geometry.ContainsLocalPoint(new Vector3(2.1f, 1f, 0f)),
                Is.False);
            Assert.That(
                geometry.ContainsLocalPoint(new Vector3(0f, -0.1f, 0f)),
                Is.False);
            Assert.That(
                geometry.ContainsLocalPoint(new Vector3(0f, 3.1f, 0f)),
                Is.False);
        }

        [Test]
        public void ExtrudedPolygon_ContainsConcaveFootprintExactly()
        {
            var geometry = new ExtrudedPolygonFloodGeometry(
                new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(2f, 0f),
                    new Vector2(2f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 2f),
                    new Vector2(0f, 2f),
                },
                4d);

            Assert.That(
                geometry.ContainmentPrecision,
                Is.EqualTo(FloodContainmentPrecision.Exact));
            Assert.That(
                geometry.ContainsLocalPoint(new Vector3(0.5f, 1f, 0.5f)),
                Is.True);
            Assert.That(
                geometry.ContainsLocalPoint(new Vector3(0.5f, 1f, 1.5f)),
                Is.True);
            Assert.That(
                geometry.ContainsLocalPoint(new Vector3(1.5f, 1f, 0.5f)),
                Is.True);
            Assert.That(
                geometry.ContainsLocalPoint(new Vector3(1.5f, 1f, 1.5f)),
                Is.False);
            Assert.That(
                geometry.ContainsLocalPoint(new Vector3(0.5f, 4.1f, 0.5f)),
                Is.False);
        }

        [Test]
        public void BakedGeometry_UsesOccupiedCellApproximation()
        {
            var data = CreatePartialOccupancyData();
            var geometry = new BakedFloodGeometry(data);

            Assert.That(
                geometry.ContainmentPrecision,
                Is.EqualTo(FloodContainmentPrecision.BakeApproximation));
            Assert.That(
                geometry.ContainsLocalPoint(new Vector3(-0.5f, -0.5f, -0.5f)),
                Is.True);
            Assert.That(
                geometry.ContainsLocalPoint(new Vector3(0.5f, -0.5f, -0.5f)),
                Is.False);
            Assert.That(
                geometry.ContainsLocalPoint(new Vector3(0f, 0f, 2f)),
                Is.False);

            Object.DestroyImmediate(data);
        }

        private static FloodVolumeData CreatePartialOccupancyData()
        {
            // Bounds center origin, size 2 → cells of size 1 cover [-1,1]^3.
            // Occupied only cell index 0 (min corner).
            var data = ScriptableObject.CreateInstance<FloodVolumeData>();
            data.Initialize(
                new Bounds(Vector3.zero, new Vector3(2f, 2f, 2f)),
                Vector3.one,
                new Vector3Int(2, 2, 2),
                new[] { 0 },
                newBoundaryCellCount: 1,
                newSourceFingerprint: "containment-test");
            return data;
        }
    }
}
