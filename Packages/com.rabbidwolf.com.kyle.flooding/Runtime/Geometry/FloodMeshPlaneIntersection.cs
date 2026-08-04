using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Shared plane ∩ triangle-mesh contour reconstruction for extruded and
    /// baked presentation-boundary free surfaces.
    /// </summary>
    internal static class FloodMeshPlaneIntersection
    {
        /// <summary>
        /// Intersects an indexed triangle mesh with a local-space plane and
        /// stitches closed surface contours.
        /// </summary>
        public static FloodSurfaceIntersection IntersectMesh(
            IReadOnlyList<Vector3> vertices,
            IReadOnlyList<int> triangles,
            Plane plane)
        {
            if (vertices == null)
                throw new ArgumentNullException(nameof(vertices));
            if (triangles == null)
                throw new ArgumentNullException(nameof(triangles));
            if (triangles.Count % 3 != 0)
            {
                throw new ArgumentException(
                    "Triangle index count must be a multiple of three.",
                    nameof(triangles));
            }

            var segments = new List<FloodSurfaceSegment>();

            for (var index = 0; index < triangles.Count; index += 3)
            {
                var firstIndex = triangles[index];
                var secondIndex = triangles[index + 1];
                var thirdIndex = triangles[index + 2];

                if (firstIndex < 0
                    || firstIndex >= vertices.Count
                    || secondIndex < 0
                    || secondIndex >= vertices.Count
                    || thirdIndex < 0
                    || thirdIndex >= vertices.Count)
                {
                    continue;
                }

                var triangle = new FloodGeometryTriangle(
                    vertices[firstIndex],
                    vertices[secondIndex],
                    vertices[thirdIndex]);

                if (TryIntersectTriangle(triangle, plane, out var segment))
                    AddUniqueSegment(segments, segment);
            }

            var contours = StitchContours(segments, plane.normal);
            return contours.Count == 0
                ? default
                : new FloodSurfaceIntersection(contours.ToArray());
        }

        /// <summary>
        /// Intersects an explicit triangle list with a local-space plane.
        /// </summary>
        public static FloodSurfaceIntersection IntersectTriangles(
            IReadOnlyList<FloodGeometryTriangle> triangles,
            Plane plane)
        {
            if (triangles == null)
                throw new ArgumentNullException(nameof(triangles));

            var segments = new List<FloodSurfaceSegment>();

            foreach (var triangle in triangles)
            {
                if (TryIntersectTriangle(triangle, plane, out var segment))
                    AddUniqueSegment(segments, segment);
            }

            var contours = StitchContours(segments, plane.normal);
            return contours.Count == 0
                ? default
                : new FloodSurfaceIntersection(contours.ToArray());
        }

        internal static bool TryIntersectTriangle(
            FloodGeometryTriangle triangle,
            Plane plane,
            out FloodSurfaceSegment segment)
        {
            var points = new[] { triangle.A, triangle.B, triangle.C };
            var distances = new[]
            {
                plane.GetDistanceToPoint(points[0]),
                plane.GetDistanceToPoint(points[1]),
                plane.GetDistanceToPoint(points[2]),
            };
            var allOnPlane = true;
            var intersections = new List<Vector3>(3);

            for (var index = 0; index < points.Length; index++)
            {
                if (Math.Abs(distances[index])
                    > FloodGeometryTolerances.Position)
                {
                    allOnPlane = false;
                }

                if (Math.Abs(distances[index])
                    <= FloodGeometryTolerances.Position)
                {
                    AddUniquePoint(intersections, points[index]);
                }

                var next = (index + 1) % points.Length;

                if ((distances[index] < -FloodGeometryTolerances.Position
                        && distances[next]
                            > FloodGeometryTolerances.Position)
                    || (distances[index]
                            > FloodGeometryTolerances.Position
                        && distances[next]
                            < -FloodGeometryTolerances.Position))
                {
                    var interpolation =
                        distances[index]
                        / (distances[index] - distances[next]);
                    AddUniquePoint(
                        intersections,
                        points[index]
                            + ((points[next] - points[index])
                                * interpolation));
                }
            }

            if (allOnPlane || intersections.Count < 2)
            {
                segment = default;
                return false;
            }

            var maximumDistance = -1f;
            var first = Vector3.zero;
            var second = Vector3.zero;

            for (var firstIndex = 0;
                 firstIndex < intersections.Count;
                 firstIndex++)
            {
                for (var secondIndex = firstIndex + 1;
                     secondIndex < intersections.Count;
                     secondIndex++)
                {
                    var distance = (
                        intersections[firstIndex]
                        - intersections[secondIndex]).sqrMagnitude;

                    if (distance <= maximumDistance)
                        continue;

                    maximumDistance = distance;
                    first = intersections[firstIndex];
                    second = intersections[secondIndex];
                }
            }

            if (maximumDistance
                <= FloodGeometryTolerances.Position
                    * FloodGeometryTolerances.Position)
            {
                segment = default;
                return false;
            }

            segment = new FloodSurfaceSegment(first, second);
            return true;
        }

        internal static List<FloodSurfaceContour> StitchContours(
            List<FloodSurfaceSegment> segments,
            Vector3 planeNormal)
        {
            var remaining = new List<FloodSurfaceSegment>(segments);
            var contours = new List<FloodSurfaceContour>();

            while (remaining.Count > 0)
            {
                var seed = remaining[0];
                remaining.RemoveAt(0);
                var points = new List<Vector3> { seed.First, seed.Second };
                var closed = false;

                while (remaining.Count > 0)
                {
                    var current = points[points.Count - 1];
                    var found = false;

                    for (var index = 0; index < remaining.Count; index++)
                    {
                        var candidate = remaining[index];
                        Vector3 next;

                        if (Approximately(current, candidate.First))
                            next = candidate.Second;
                        else if (Approximately(current, candidate.Second))
                            next = candidate.First;
                        else
                            continue;

                        remaining.RemoveAt(index);
                        found = true;

                        if (Approximately(next, points[0]))
                            closed = true;
                        else
                            points.Add(next);

                        break;
                    }

                    if (closed || !found)
                        break;
                }

                if (!closed || points.Count < 3)
                    continue;

                SimplifyContour(points);
                OrientContour(points, planeNormal);

                if (points.Count >= 3)
                {
                    contours.Add(
                        new FloodSurfaceContour(points.ToArray()));
                }
            }

            return contours;
        }

        internal static void AddUniqueSegment(
            List<FloodSurfaceSegment> segments,
            FloodSurfaceSegment candidate)
        {
            foreach (var segment in segments)
            {
                if ((Approximately(segment.First, candidate.First)
                        && Approximately(segment.Second, candidate.Second))
                    || (Approximately(segment.First, candidate.Second)
                        && Approximately(segment.Second, candidate.First)))
                {
                    return;
                }
            }

            segments.Add(candidate);
        }

        internal static void AddUniquePoint(
            List<Vector3> points,
            Vector3 candidate)
        {
            foreach (var point in points)
            {
                if (Approximately(point, candidate))
                    return;
            }

            points.Add(candidate);
        }

        internal static bool Approximately(Vector3 first, Vector3 second)
        {
            return (first - second).sqrMagnitude
                <= FloodGeometryTolerances.Position
                    * FloodGeometryTolerances.Position;
        }

        internal static void CreatePlaneBasis(
            Vector3 normal,
            out Vector3 tangent,
            out Vector3 bitangent)
        {
            normal.Normalize();
            tangent = Math.Abs(normal.y) < 0.9f
                ? Vector3.Cross(normal, Vector3.up).normalized
                : Vector3.Cross(normal, Vector3.right).normalized;
            bitangent = Vector3.Cross(normal, tangent).normalized;
        }

        private static void SimplifyContour(List<Vector3> points)
        {
            var removed = true;

            while (removed && points.Count > 3)
            {
                removed = false;

                for (var index = 0; index < points.Count; index++)
                {
                    var previous =
                        points[(index - 1 + points.Count) % points.Count];
                    var current = points[index];
                    var next = points[(index + 1) % points.Count];
                    var firstDirection = current - previous;
                    var secondDirection = next - current;

                    if (Vector3.Cross(firstDirection, secondDirection).magnitude
                        > FloodGeometryTolerances.Position)
                    {
                        continue;
                    }

                    points.RemoveAt(index);
                    removed = true;
                    break;
                }
            }
        }

        private static void OrientContour(
            List<Vector3> points,
            Vector3 planeNormal)
        {
            CreatePlaneBasis(
                planeNormal,
                out var tangent,
                out var bitangent);
            var twiceArea = 0d;

            for (var index = 0; index < points.Count; index++)
            {
                var next = (index + 1) % points.Count;
                var firstX = Vector3.Dot(points[index], tangent);
                var firstY = Vector3.Dot(points[index], bitangent);
                var secondX = Vector3.Dot(points[next], tangent);
                var secondY = Vector3.Dot(points[next], bitangent);
                twiceArea +=
                    ((double)firstX * secondY)
                    - ((double)secondX * firstY);
            }

            if (twiceArea < 0d)
                points.Reverse();
        }
    }

    internal readonly struct FloodGeometryTriangle
    {
        public FloodGeometryTriangle(
            Vector3 first,
            Vector3 second,
            Vector3 third)
        {
            A = first;
            B = second;
            C = third;
        }

        public Vector3 A { get; }
        public Vector3 B { get; }
        public Vector3 C { get; }
    }

    internal readonly struct FloodSurfaceSegment
    {
        public FloodSurfaceSegment(Vector3 first, Vector3 second)
        {
            First = first;
            Second = second;
        }

        public Vector3 First { get; }
        public Vector3 Second { get; }
    }
}
