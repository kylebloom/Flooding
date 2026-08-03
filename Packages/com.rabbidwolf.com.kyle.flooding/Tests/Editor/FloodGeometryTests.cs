using System;
using NUnit.Framework;
using UnityEngine;

namespace Kyle.Flooding.Tests
{
    public sealed class FloodGeometryTests
    {
        [Test]
        public void Rectangle_EvaluatesHorizontalSubmersion()
        {
            var geometry =
                new RectangularPrismFloodGeometry(4d, 3d, 2d);
            var plane = new Plane(Vector3.up, new Vector3(0f, 0.5f, 0f));

            var result = geometry.EvaluateSubmersion(plane);

            Assert.That(geometry.Capacity, Is.EqualTo(24d));
            Assert.That(result.Volume, Is.EqualTo(6d).Within(0.000001d));
            Assert.That(result.Centroid.x, Is.Zero.Within(0.000001f));
            Assert.That(
                result.Centroid.y,
                Is.EqualTo(0.25f).Within(0.000001f));
            Assert.That(result.Centroid.z, Is.Zero.Within(0.000001f));
            Assert.That(result.SurfaceIntersection.HasSurface, Is.True);
            Assert.That(
                result.SurfaceIntersection.Contours[0].Vertices.Count,
                Is.EqualTo(4));
        }

        [Test]
        public void Rectangle_EvaluatesTiltedPlaneExactly()
        {
            var geometry =
                new RectangularPrismFloodGeometry(2d, 2d, 2d);
            var plane = new Plane(
                new Vector3(1f, 1f, 0f).normalized,
                new Vector3(0f, 1f, 0f));

            var result = geometry.EvaluateSubmersion(plane);

            Assert.That(geometry.SupportsPlane(plane), Is.True);
            Assert.That(result.Volume, Is.EqualTo(4d).Within(0.00001d));
            Assert.That(result.SurfaceIntersection.HasSurface, Is.True);
            Assert.That(
                result.SurfaceIntersection.Contours[0].Vertices.Count,
                Is.EqualTo(4));
        }

        [Test]
        public void SurfaceSolver_MatchesRequestedVolumeForTiltedRectangle()
        {
            var geometry =
                new RectangularPrismFloodGeometry(4d, 3d, 2d);
            var solution = FloodSurfaceSolver.Solve(
                geometry,
                new Vector3(0.4f, 1f, -0.25f).normalized,
                targetVolume: 7.25d);

            Assert.That(
                solution.Submersion.Volume,
                Is.EqualTo(7.25d).Within(0.000025d));
            Assert.That(
                Math.Abs(solution.VolumeError),
                Is.LessThanOrEqualTo(0.000025d));
            Assert.That(
                Vector3.Dot(
                    solution.LocalSurfacePlane.normal,
                    new Vector3(0.4f, 1f, -0.25f).normalized),
                Is.GreaterThan(0.999999f));
            Assert.That(
                solution.Iterations,
                Is.LessThanOrEqualTo(
                    FloodGeometryTolerances.SolverMaximumIterations));
        }

        [Test]
        public void ConcavePolygon_TiltedSolvePreservesTargetVolume()
        {
            var geometry = new ExtrudedPolygonFloodGeometry(
                new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(3f, 0f),
                    new Vector2(3f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, 3f),
                    new Vector2(0f, 3f),
                },
                2.5d);
            var solution = FloodSurfaceSolver.Solve(
                geometry,
                new Vector3(-0.5f, 0.7f, 0.3f).normalized,
                targetVolume: 5.5d);

            Assert.That(
                solution.Submersion.Volume,
                Is.EqualTo(5.5d).Within(0.000025d));
            Assert.That(solution.Submersion.SurfaceIntersection.HasSurface, Is.True);
            Assert.That(solution.Submersion.Centroid.x, Is.GreaterThanOrEqualTo(0f));
            Assert.That(solution.Submersion.Centroid.y, Is.InRange(0f, 2.5f));
            Assert.That(solution.Submersion.Centroid.z, Is.GreaterThanOrEqualTo(0f));
        }

        [Test]
        public void ConcavePolygon_HasStableAreaCentroidAndFill()
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
            var plane = new Plane(Vector3.up, new Vector3(0f, 2f, 0f));

            var result = geometry.EvaluateSubmersion(plane);

            Assert.That(geometry.FloorArea, Is.EqualTo(3d).Within(0.000001d));
            Assert.That(geometry.Capacity, Is.EqualTo(12d).Within(0.000001d));
            Assert.That(result.Volume, Is.EqualTo(6d).Within(0.000001d));
            Assert.That(result.Centroid.x, Is.EqualTo(5f / 6f).Within(0.000001f));
            Assert.That(result.Centroid.y, Is.EqualTo(1f).Within(0.000001f));
            Assert.That(result.Centroid.z, Is.EqualTo(5f / 6f).Within(0.000001f));
            Assert.That(geometry.SurfaceTriangles.Count, Is.EqualTo(12));
        }

        [Test]
        public void ClockwisePolygon_IsAcceptedAndNormalized()
        {
            var geometry = new ExtrudedPolygonFloodGeometry(
                new[]
                {
                    new Vector2(-1f, -1f),
                    new Vector2(-1f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(1f, -1f),
                },
                2d);

            var signedArea = 0d;

            for (var index = 0; index < geometry.Footprint.Count; index++)
            {
                var next = (index + 1) % geometry.Footprint.Count;
                signedArea +=
                    ((double)geometry.Footprint[index].x
                        * geometry.Footprint[next].y)
                    - ((double)geometry.Footprint[next].x
                        * geometry.Footprint[index].y);
            }

            Assert.That(signedArea, Is.GreaterThan(0d));
            Assert.That(geometry.Capacity, Is.EqualTo(8d));
        }

        [Test]
        public void SelfIntersectingPolygon_IsRejectedWithActionableMessage()
        {
            var footprint = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(3f, 0f),
                new Vector2(0f, 2f),
                new Vector2(2f, 2f),
            };

            var isValid = FloodPolygonValidation.TryValidate(
                footprint,
                out _,
                out var message);

            Assert.That(isValid, Is.False);
            Assert.That(message, Does.Contain("intersect"));
            Assert.Throws<ArgumentException>(
                () => new ExtrudedPolygonFloodGeometry(footprint, 2d));
        }

        [Test]
        public void DuplicatePolygonPoint_IsRejectedWithPointIndices()
        {
            var isValid = FloodPolygonValidation.TryValidate(
                new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 0f),
                },
                out _,
                out var message);

            Assert.That(isValid, Is.False);
            Assert.That(message, Does.Contain("0"));
            Assert.That(message, Does.Contain("3"));
        }
    }
}
