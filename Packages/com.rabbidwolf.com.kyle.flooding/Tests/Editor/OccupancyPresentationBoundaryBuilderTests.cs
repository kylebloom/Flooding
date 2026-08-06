using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Kyle.Flooding.Tests
{
    public sealed class OccupancyPresentationBoundaryBuilderTests
    {
        [Test]
        public void SingleCell_EmitsSixFaces()
        {
            Assert.That(
                OccupancyPresentationBoundaryBuilder.TryBuild(
                    new Bounds(Vector3.one * 0.5f, Vector3.one),
                    Vector3.one,
                    new Vector3Int(1, 1, 1),
                    new[] { 0 },
                    out var vertices,
                    out var triangles,
                    out var message),
                Is.True,
                message);

            Assert.That(vertices.Length, Is.EqualTo(8));
            Assert.That(triangles.Length / 3, Is.EqualTo(12));
        }

        [Test]
        public void TwoFaceAdjacentCells_OmitSharedFace()
        {
            Assert.That(
                OccupancyPresentationBoundaryBuilder.TryBuild(
                    new Bounds(new Vector3(1f, 0.5f, 0.5f), new Vector3(2f, 1f, 1f)),
                    Vector3.one,
                    new Vector3Int(2, 1, 1),
                    new[] { 0, 1 },
                    out _,
                    out var triangles,
                    out var message),
                Is.True,
                message);

            // 10 exterior faces × 2 triangles (not 12×2 with shared face kept).
            Assert.That(triangles.Length / 3, Is.EqualTo(20));
        }

        [Test]
        public void OverlappingDuplicateIndex_StillOmitsInterior()
        {
            // Deduped occupancy still has one cell — builder receives unique set
            // from bake, but tolerate duplicate list entries.
            Assert.That(
                OccupancyPresentationBoundaryBuilder.TryBuild(
                    new Bounds(Vector3.one * 0.5f, Vector3.one),
                    Vector3.one,
                    new Vector3Int(1, 1, 1),
                    new[] { 0, 0 },
                    out _,
                    out var triangles,
                    out var message),
                Is.True,
                message);

            Assert.That(triangles.Length / 3, Is.EqualTo(12));
        }

        [Test]
        public void ConcaveLFootprint_EarClipDoesNotFanOutsideNotch()
        {
            // Planar L: (0,0)-(2,0)-(2,1)-(1,1)-(1,2)-(0,2) in XY, Z=0.
            var contour = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(2f, 0f, 0f),
                new Vector3(2f, 1f, 0f),
                new Vector3(1f, 1f, 0f),
                new Vector3(1f, 2f, 0f),
                new Vector3(0f, 2f, 0f),
            };
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            FloodPlanarPolygonTriangulation.AppendContour(
                contour,
                Vector3.forward,
                vertices,
                triangles);

            Assert.That(triangles.Count / 3, Is.EqualTo(4));

            // Notch exterior sample (1.5, 1.5) must not be covered by any triangle.
            var outsideNotch = new Vector3(1.5f, 1.5f, 0f);
            Assert.That(
                AnyTriangleContainsPoint(vertices, triangles, outsideNotch),
                Is.False);
        }

        private static bool AnyTriangleContainsPoint(
            List<Vector3> vertices,
            List<int> triangles,
            Vector3 point)
        {
            for (var index = 0; index < triangles.Count; index += 3)
            {
                var a = vertices[triangles[index]];
                var b = vertices[triangles[index + 1]];
                var c = vertices[triangles[index + 2]];
                if (PointInTriangle2D(point, a, b, c))
                    return true;
            }

            return false;
        }

        private static bool PointInTriangle2D(
            Vector3 point,
            Vector3 a,
            Vector3 b,
            Vector3 c)
        {
            // Barycentric in XY (contour is z=0).
            var v0x = c.x - a.x;
            var v0y = c.y - a.y;
            var v1x = b.x - a.x;
            var v1y = b.y - a.y;
            var v2x = point.x - a.x;
            var v2y = point.y - a.y;
            var dot00 = v0x * v0x + v0y * v0y;
            var dot01 = v0x * v1x + v0y * v1y;
            var dot02 = v0x * v2x + v0y * v2y;
            var dot11 = v1x * v1x + v1y * v1y;
            var dot12 = v1x * v2x + v1y * v2y;
            var denom = dot00 * dot11 - dot01 * dot01;
            if (Mathf.Abs(denom) < 1e-8f)
                return false;
            var u = (dot11 * dot02 - dot01 * dot12) / denom;
            var v = (dot00 * dot12 - dot01 * dot02) / denom;
            return u >= -1e-4f && v >= -1e-4f && (u + v) <= 1f + 1e-4f;
        }
    }
}
