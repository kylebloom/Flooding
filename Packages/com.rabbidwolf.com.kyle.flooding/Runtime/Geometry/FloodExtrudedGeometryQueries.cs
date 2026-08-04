using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Exact clipped-polyhedron queries for vertical polygon prisms.
    /// </summary>
    internal static class FloodExtrudedGeometryQueries
    {
        private static readonly int[][] TetrahedronFaces =
        {
            new[] { 0, 1, 2 },
            new[] { 0, 3, 1 },
            new[] { 0, 2, 3 },
            new[] { 1, 3, 2 },
        };

        public static FloodSubmersionResult Evaluate(
            IExtrudedFloodVolumeGeometry geometry,
            Plane localSurfacePlane,
            bool includeSurfaceIntersection)
        {
            ValidatePlane(localSurfacePlane);

            var totalVolume = 0d;
            var weightedCentroid = Vector3.zero;
            var footprint = geometry.Footprint;
            var triangles = geometry.SurfaceTriangles;
            var height = (float)geometry.MaximumHeight;

            for (var index = 0; index < triangles.Count; index += 3)
            {
                var first = footprint[triangles[index]];
                var second = footprint[triangles[index + 1]];
                var third = footprint[triangles[index + 2]];
                var bottomFirst = ToLocalPoint(first, 0f);
                var bottomSecond = ToLocalPoint(second, 0f);
                var bottomThird = ToLocalPoint(third, 0f);
                var topFirst = ToLocalPoint(first, height);
                var topSecond = ToLocalPoint(second, height);
                var topThird = ToLocalPoint(third, height);

                AccumulateClippedTetrahedron(
                    bottomFirst,
                    bottomSecond,
                    bottomThird,
                    topThird,
                    localSurfacePlane,
                    ref totalVolume,
                    ref weightedCentroid);
                AccumulateClippedTetrahedron(
                    bottomFirst,
                    bottomSecond,
                    topSecond,
                    topThird,
                    localSurfacePlane,
                    ref totalVolume,
                    ref weightedCentroid);
                AccumulateClippedTetrahedron(
                    bottomFirst,
                    topFirst,
                    topSecond,
                    topThird,
                    localSurfacePlane,
                    ref totalVolume,
                    ref weightedCentroid);
            }

            totalVolume = Math.Max(0d, Math.Min(geometry.Capacity, totalVolume));

            var centroid = totalVolume
                    > FloodGeometryTolerances.SolverAbsoluteVolume
                ? weightedCentroid / (float)totalVolume
                : new Vector3(
                    geometry.FootprintCentroid.x,
                    0f,
                    geometry.FootprintCentroid.y);
            var intersection = includeSurfaceIntersection
                ? BuildSurfaceIntersection(
                    geometry,
                    localSurfacePlane,
                    totalVolume)
                : default;

            return new FloodSubmersionResult(
                totalVolume,
                centroid,
                intersection);
        }

        public static FloodMeshData BuildSubmergedMesh(
            IExtrudedFloodVolumeGeometry geometry,
            Plane localSurfacePlane)
        {
            var evaluation = Evaluate(
                geometry,
                localSurfacePlane,
                includeSurfaceIntersection: true);
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var boundary = BuildBoundaryTriangles(geometry);

            foreach (var triangle in boundary)
            {
                var clipped = ClipPolygon(
                    new[] { triangle.A, triangle.B, triangle.C },
                    localSurfacePlane,
                    capPoints: null);
                AddPolygon(clipped, vertices, triangles);
            }

            var volumeTolerance = Math.Max(
                FloodGeometryTolerances.SolverAbsoluteVolume,
                geometry.Capacity
                    * FloodGeometryTolerances.SolverRelativeVolume);

            if (evaluation.Volume > volumeTolerance
                && evaluation.Volume < geometry.Capacity - volumeTolerance)
            {
                foreach (var contour
                    in evaluation.SurfaceIntersection.Contours)
                {
                    AddSurfaceContour(
                        contour.Vertices,
                        localSurfacePlane.normal,
                        vertices,
                        triangles);
                }
            }

            return new FloodMeshData(
                vertices.ToArray(),
                triangles.ToArray());
        }

        private static void AccumulateClippedTetrahedron(
            Vector3 first,
            Vector3 second,
            Vector3 third,
            Vector3 fourth,
            Plane plane,
            ref double totalVolume,
            ref Vector3 weightedCentroid)
        {
            var tetrahedron = new[] { first, second, third, fourth };
            var insideCount = 0;

            for (var index = 0; index < tetrahedron.Length; index++)
            {
                if (plane.GetDistanceToPoint(tetrahedron[index])
                    <= FloodGeometryTolerances.Position)
                {
                    insideCount++;
                }
            }

            if (insideCount == 0)
                return;

            if (insideCount == 4)
            {
                AccumulateTetrahedron(
                    first,
                    second,
                    third,
                    fourth,
                    ref totalVolume,
                    ref weightedCentroid);
                return;
            }

            var faces = new List<List<Vector3>>(5);
            var capPoints = new List<Vector3>(4);

            foreach (var face in TetrahedronFaces)
            {
                var clipped = ClipPolygon(
                    new[]
                    {
                        tetrahedron[face[0]],
                        tetrahedron[face[1]],
                        tetrahedron[face[2]],
                    },
                    plane,
                    capPoints);

                if (clipped.Count >= 3)
                    faces.Add(clipped);
            }

            if (capPoints.Count >= 3)
            {
                SortPointsOnPlane(capPoints, plane.normal);
                faces.Add(capPoints);
            }

            var uniqueVertices = new List<Vector3>(8);

            foreach (var face in faces)
            {
                foreach (var point in face)
                    FloodMeshPlaneIntersection.AddUniquePoint(
                        uniqueVertices,
                        point);
            }

            if (uniqueVertices.Count < 4)
                return;

            var interiorPoint = Vector3.zero;

            foreach (var point in uniqueVertices)
                interiorPoint += point;

            interiorPoint /= uniqueVertices.Count;

            foreach (var face in faces)
            {
                for (var index = 1; index < face.Count - 1; index++)
                {
                    AccumulateTetrahedron(
                        interiorPoint,
                        face[0],
                        face[index],
                        face[index + 1],
                        ref totalVolume,
                        ref weightedCentroid);
                }
            }
        }

        private static void AccumulateTetrahedron(
            Vector3 first,
            Vector3 second,
            Vector3 third,
            Vector3 fourth,
            ref double totalVolume,
            ref Vector3 weightedCentroid)
        {
            var signedSixVolume = Vector3.Dot(
                second - first,
                Vector3.Cross(third - first, fourth - first));
            var volume = Math.Abs((double)signedSixVolume) / 6d;

            if (volume <= double.Epsilon)
                return;

            var centroid = (first + second + third + fourth) * 0.25f;
            totalVolume += volume;
            weightedCentroid += centroid * (float)volume;
        }

        private static List<Vector3> ClipPolygon(
            IReadOnlyList<Vector3> polygon,
            Plane plane,
            List<Vector3> capPoints)
        {
            var result = new List<Vector3>(polygon.Count + 1);

            for (var index = 0; index < polygon.Count; index++)
            {
                var current = polygon[index];
                var next = polygon[(index + 1) % polygon.Count];
                var currentDistance = plane.GetDistanceToPoint(current);
                var nextDistance = plane.GetDistanceToPoint(next);
                var currentInside =
                    currentDistance <= FloodGeometryTolerances.Position;
                var nextInside =
                    nextDistance <= FloodGeometryTolerances.Position;

                if (currentInside)
                    FloodMeshPlaneIntersection.AddUniquePoint(result, current);

                if (currentInside == nextInside)
                    continue;

                var denominator = currentDistance - nextDistance;

                if (Math.Abs(denominator)
                    <= FloodGeometryTolerances.Position)
                {
                    continue;
                }

                var interpolation = currentDistance / denominator;
                var intersection =
                    current + ((next - current) * interpolation);
                FloodMeshPlaneIntersection.AddUniquePoint(result, intersection);

                if (capPoints != null)
                {
                    FloodMeshPlaneIntersection.AddUniquePoint(
                        capPoints,
                        intersection);
                }
            }

            if (capPoints != null)
            {
                foreach (var point in result)
                {
                    if (Math.Abs(plane.GetDistanceToPoint(point))
                        <= FloodGeometryTolerances.Position)
                    {
                        FloodMeshPlaneIntersection.AddUniquePoint(
                            capPoints,
                            point);
                    }
                }
            }

            return result;
        }

        private static FloodSurfaceIntersection BuildSurfaceIntersection(
            IExtrudedFloodVolumeGeometry geometry,
            Plane plane,
            double volume)
        {
            var volumeTolerance = Math.Max(
                FloodGeometryTolerances.SolverAbsoluteVolume,
                geometry.Capacity
                    * FloodGeometryTolerances.SolverRelativeVolume);

            if (volume <= volumeTolerance
                || volume >= geometry.Capacity - volumeTolerance)
            {
                return default;
            }

            return FloodMeshPlaneIntersection.IntersectTriangles(
                BuildBoundaryTriangles(geometry),
                plane);
        }

        private static List<FloodGeometryTriangle> BuildBoundaryTriangles(
            IExtrudedFloodVolumeGeometry geometry)
        {
            var result = new List<FloodGeometryTriangle>();
            var footprint = geometry.Footprint;
            var surfaceTriangles = geometry.SurfaceTriangles;
            var height = (float)geometry.MaximumHeight;

            for (var index = 0; index < surfaceTriangles.Count; index += 3)
            {
                var first = surfaceTriangles[index];
                var second = surfaceTriangles[index + 1];
                var third = surfaceTriangles[index + 2];
                var bottomFirst = ToLocalPoint(footprint[first], 0f);
                var bottomSecond = ToLocalPoint(footprint[second], 0f);
                var bottomThird = ToLocalPoint(footprint[third], 0f);
                var topFirst = ToLocalPoint(footprint[first], height);
                var topSecond = ToLocalPoint(footprint[second], height);
                var topThird = ToLocalPoint(footprint[third], height);

                result.Add(
                    new FloodGeometryTriangle(
                        bottomFirst,
                        bottomSecond,
                        bottomThird));
                result.Add(
                    new FloodGeometryTriangle(
                        topFirst,
                        topThird,
                        topSecond));
            }

            for (var index = 0; index < footprint.Count; index++)
            {
                var next = (index + 1) % footprint.Count;
                var bottomFirst = ToLocalPoint(footprint[index], 0f);
                var bottomNext = ToLocalPoint(footprint[next], 0f);
                var topFirst = ToLocalPoint(footprint[index], height);
                var topNext = ToLocalPoint(footprint[next], height);

                result.Add(
                    new FloodGeometryTriangle(
                        bottomFirst,
                        topNext,
                        bottomNext));
                result.Add(
                    new FloodGeometryTriangle(
                        bottomFirst,
                        topFirst,
                        topNext));
            }

            return result;
        }

        private static void AddSurfaceContour(
            IReadOnlyList<Vector3> contour,
            Vector3 planeNormal,
            List<Vector3> vertices,
            List<int> triangles)
        {
            if (contour.Count < 3)
                return;

            FloodMeshPlaneIntersection.CreatePlaneBasis(
                planeNormal,
                out var tangent,
                out var bitangent);
            var projected = new Vector2[contour.Count];

            for (var index = 0; index < contour.Count; index++)
            {
                projected[index] = new Vector2(
                    Vector3.Dot(contour[index], tangent),
                    Vector3.Dot(contour[index], bitangent));
            }

            var surfaceTriangles = Triangulate(projected);
            var vertexOffset = vertices.Count;

            for (var index = 0; index < contour.Count; index++)
                vertices.Add(contour[index]);

            foreach (var triangleIndex in surfaceTriangles)
                triangles.Add(vertexOffset + triangleIndex);
        }

        private static int[] Triangulate(IReadOnlyList<Vector2> polygon)
        {
            var remaining = new List<int>(polygon.Count);
            var triangles = new List<int>((polygon.Count - 2) * 3);

            for (var index = 0; index < polygon.Count; index++)
                remaining.Add(index);

            var attempts = 0;
            var maximumAttempts = polygon.Count * polygon.Count;

            while (remaining.Count > 3 && attempts < maximumAttempts)
            {
                var clipped = false;

                for (var index = 0; index < remaining.Count; index++)
                {
                    var previous =
                        remaining[(index - 1 + remaining.Count)
                            % remaining.Count];
                    var current = remaining[index];
                    var next = remaining[(index + 1) % remaining.Count];

                    if (Cross(
                            polygon[previous],
                            polygon[current],
                            polygon[next])
                        <= FloodGeometryTolerances.Position)
                    {
                        continue;
                    }

                    var containsPoint = false;

                    for (var candidateIndex = 0;
                         candidateIndex < remaining.Count;
                         candidateIndex++)
                    {
                        var candidate = remaining[candidateIndex];

                        if (candidate == previous
                            || candidate == current
                            || candidate == next)
                        {
                            continue;
                        }

                        if (IsPointInTriangle(
                                polygon[candidate],
                                polygon[previous],
                                polygon[current],
                                polygon[next]))
                        {
                            containsPoint = true;
                            break;
                        }
                    }

                    if (containsPoint)
                        continue;

                    triangles.Add(previous);
                    triangles.Add(current);
                    triangles.Add(next);
                    remaining.RemoveAt(index);
                    clipped = true;
                    break;
                }

                if (!clipped)
                    return Array.Empty<int>();

                attempts++;
            }

            if (remaining.Count == 3)
            {
                triangles.Add(remaining[0]);
                triangles.Add(remaining[1]);
                triangles.Add(remaining[2]);
            }

            return triangles.ToArray();
        }

        private static bool IsPointInTriangle(
            Vector2 point,
            Vector2 first,
            Vector2 second,
            Vector2 third)
        {
            var tolerance = -FloodGeometryTolerances.Position;

            return Cross(first, second, point) >= tolerance
                && Cross(second, third, point) >= tolerance
                && Cross(third, first, point) >= tolerance;
        }

        private static double Cross(
            Vector2 first,
            Vector2 second,
            Vector2 third)
        {
            return ((double)second.x - first.x)
                    * ((double)third.y - first.y)
                - ((double)second.y - first.y)
                    * ((double)third.x - first.x);
        }

        private static void AddPolygon(
            IReadOnlyList<Vector3> polygon,
            List<Vector3> vertices,
            List<int> triangles)
        {
            if (polygon.Count < 3)
                return;

            var offset = vertices.Count;

            for (var index = 0; index < polygon.Count; index++)
                vertices.Add(polygon[index]);

            for (var index = 1; index < polygon.Count - 1; index++)
            {
                triangles.Add(offset);
                triangles.Add(offset + index);
                triangles.Add(offset + index + 1);
            }
        }

        private static void SortPointsOnPlane(
            List<Vector3> points,
            Vector3 normal)
        {
            var center = Vector3.zero;

            foreach (var point in points)
                center += point;

            center /= points.Count;
            FloodMeshPlaneIntersection.CreatePlaneBasis(
                normal,
                out var tangent,
                out var bitangent);
            points.Sort(
                (first, second) =>
                {
                    var firstOffset = first - center;
                    var secondOffset = second - center;
                    var firstAngle = Math.Atan2(
                        Vector3.Dot(firstOffset, bitangent),
                        Vector3.Dot(firstOffset, tangent));
                    var secondAngle = Math.Atan2(
                        Vector3.Dot(secondOffset, bitangent),
                        Vector3.Dot(secondOffset, tangent));
                    return firstAngle.CompareTo(secondAngle);
                });
        }

        private static Vector3 ToLocalPoint(Vector2 footprintPoint, float y)
        {
            return new Vector3(footprintPoint.x, y, footprintPoint.y);
        }

        private static void ValidatePlane(Plane plane)
        {
            var normal = plane.normal;

            if (float.IsNaN(normal.x)
                || float.IsNaN(normal.y)
                || float.IsNaN(normal.z)
                || float.IsInfinity(normal.x)
                || float.IsInfinity(normal.y)
                || float.IsInfinity(normal.z)
                || normal.sqrMagnitude
                    <= FloodGeometryTolerances.PlaneNormal
                        * FloodGeometryTolerances.PlaneNormal
                || float.IsNaN(plane.distance)
                || float.IsInfinity(plane.distance))
            {
                throw new ArgumentException(
                    "Surface plane must contain a finite non-zero normal and "
                    + "finite distance.",
                    nameof(plane));
            }
        }
    }

    internal readonly struct FloodMeshData
    {
        public FloodMeshData(Vector3[] vertices, int[] triangles)
        {
            Vertices = vertices;
            Triangles = triangles;
        }

        public Vector3[] Vertices { get; }
        public int[] Triangles { get; }
    }
}
