using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Kyle.Flooding.Tests
{
    public sealed class FloodMeshPlaneIntersectionTests
    {
        [Test]
        public void IntersectMesh_HorizontalPlane_OnHexPrism_ReturnsSixSidedContour()
        {
            BuildHexPrism(
                out var vertices,
                out var triangles,
                height: 2f,
                radius: 1f);

            var plane = new Plane(Vector3.up, new Vector3(0f, 0.5f, 0f));
            var intersection = FloodMeshPlaneIntersection.IntersectMesh(
                vertices,
                triangles,
                plane);

            Assert.That(intersection.HasSurface, Is.True);
            Assert.That(intersection.Contours.Count, Is.EqualTo(1));
            Assert.That(intersection.Contours[0].Vertices.Count, Is.EqualTo(6));

            foreach (var point in intersection.Contours[0].Vertices)
            {
                Assert.That(
                    Mathf.Abs(plane.GetDistanceToPoint(point)),
                    Is.LessThanOrEqualTo(FloodGeometryTolerances.Position));
            }
        }

        [Test]
        public void IntersectMesh_AngledPlane_ReturnsClosedOnPlaneContour()
        {
            BuildHexPrism(
                out var vertices,
                out var triangles,
                height: 2f,
                radius: 1f);

            var normal = new Vector3(0.25f, 1f, 0.15f).normalized;
            var plane = new Plane(normal, Vector3.zero);
            var first = FloodMeshPlaneIntersection.IntersectMesh(
                vertices,
                triangles,
                plane);
            var second = FloodMeshPlaneIntersection.IntersectMesh(
                vertices,
                triangles,
                plane);

            Assert.That(first.HasSurface, Is.True);
            Assert.That(first.Contours.Count, Is.EqualTo(1));
            Assert.That(first.Contours[0].Vertices.Count, Is.GreaterThanOrEqualTo(3));

            var contour = first.Contours[0].Vertices;
            for (var index = 0; index < contour.Count; index++)
            {
                Assert.That(
                    Mathf.Abs(plane.GetDistanceToPoint(contour[index])),
                    Is.LessThanOrEqualTo(FloodGeometryTolerances.Position * 2f));

                var next = contour[(index + 1) % contour.Count];
                Assert.That(
                    (contour[index] - next).sqrMagnitude,
                    Is.GreaterThan(
                        FloodGeometryTolerances.Position
                        * FloodGeometryTolerances.Position));
            }

            Assert.That(
                second.Contours[0].Vertices.Count,
                Is.EqualTo(first.Contours[0].Vertices.Count));
            for (var index = 0; index < contour.Count; index++)
            {
                Assert.That(
                    second.Contours[0].Vertices[index],
                    Is.EqualTo(first.Contours[0].Vertices[index]));
            }
        }

        [Test]
        public void IntersectMesh_TwoWells_ReturnsTwoContours()
        {
            BuildTwoWells(
                out var vertices,
                out var triangles,
                height: 1f);

            var plane = new Plane(Vector3.up, new Vector3(0f, 0.5f, 0f));
            var intersection = FloodMeshPlaneIntersection.IntersectMesh(
                vertices,
                triangles,
                plane);

            Assert.That(intersection.HasSurface, Is.True);
            Assert.That(intersection.Contours.Count, Is.EqualTo(2));
            Assert.That(intersection.Contours[0].Vertices.Count, Is.GreaterThanOrEqualTo(3));
            Assert.That(intersection.Contours[1].Vertices.Count, Is.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void IntersectMesh_FullyCoplanarTriangle_ProducesNoSegment()
        {
            var vertices = new[]
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(1f, 0f, 0f),
                new Vector3(0f, 0f, 1f),
            };
            var triangles = new[] { 0, 1, 2 };
            var plane = new Plane(Vector3.up, Vector3.zero);

            var intersection = FloodMeshPlaneIntersection.IntersectMesh(
                vertices,
                triangles,
                plane);

            Assert.That(intersection.HasSurface, Is.False);
        }

        private static void BuildHexPrism(
            out Vector3[] vertices,
            out int[] triangles,
            float height,
            float radius)
        {
            var top = height * 0.5f;
            var bottom = -height * 0.5f;
            var points = new List<Vector3>(14);
            for (var index = 0; index < 6; index++)
            {
                var angle = index * Mathf.PI / 3f;
                var x = Mathf.Cos(angle) * radius;
                var z = Mathf.Sin(angle) * radius;
                points.Add(new Vector3(x, bottom, z));
            }

            for (var index = 0; index < 6; index++)
            {
                var angle = index * Mathf.PI / 3f;
                var x = Mathf.Cos(angle) * radius;
                var z = Mathf.Sin(angle) * radius;
                points.Add(new Vector3(x, top, z));
            }

            var indices = new List<int>();
            for (var index = 0; index < 6; index++)
            {
                var next = (index + 1) % 6;
                indices.Add(index);
                indices.Add(next);
                indices.Add(6 + next);
                indices.Add(index);
                indices.Add(6 + next);
                indices.Add(6 + index);
            }

            for (var index = 1; index < 5; index++)
            {
                indices.Add(0);
                indices.Add(index + 1);
                indices.Add(index);
                indices.Add(6);
                indices.Add(6 + index);
                indices.Add(6 + index + 1);
            }

            vertices = points.ToArray();
            triangles = indices.ToArray();
        }

        private static void BuildTwoWells(
            out Vector3[] vertices,
            out int[] triangles,
            float height)
        {
            var left = BuildBoxMesh(
                new Vector3(-1.5f, 0f, 0f),
                new Vector3(1f, height, 1f));
            var right = BuildBoxMesh(
                new Vector3(1.5f, 0f, 0f),
                new Vector3(1f, height, 1f));

            var combinedVertices = new List<Vector3>();
            var combinedTriangles = new List<int>();
            AppendMesh(left.vertices, left.triangles, combinedVertices, combinedTriangles);
            AppendMesh(right.vertices, right.triangles, combinedVertices, combinedTriangles);
            vertices = combinedVertices.ToArray();
            triangles = combinedTriangles.ToArray();
        }

        private static (Vector3[] vertices, int[] triangles) BuildBoxMesh(
            Vector3 center,
            Vector3 size)
        {
            var half = size * 0.5f;
            var vertices = new[]
            {
                center + new Vector3(-half.x, -half.y, -half.z),
                center + new Vector3(half.x, -half.y, -half.z),
                center + new Vector3(half.x, half.y, -half.z),
                center + new Vector3(-half.x, half.y, -half.z),
                center + new Vector3(-half.x, -half.y, half.z),
                center + new Vector3(half.x, -half.y, half.z),
                center + new Vector3(half.x, half.y, half.z),
                center + new Vector3(-half.x, half.y, half.z),
            };
            var triangles = new[]
            {
                0, 2, 1, 0, 3, 2,
                4, 5, 6, 4, 6, 7,
                0, 1, 5, 0, 5, 4,
                3, 7, 6, 3, 6, 2,
                0, 4, 7, 0, 7, 3,
                1, 2, 6, 1, 6, 5,
            };
            return (vertices, triangles);
        }

        private static void AppendMesh(
            Vector3[] vertices,
            int[] triangles,
            List<Vector3> destinationVertices,
            List<int> destinationTriangles)
        {
            var offset = destinationVertices.Count;
            destinationVertices.AddRange(vertices);
            foreach (var index in triangles)
                destinationTriangles.Add(offset + index);
        }
    }
}
