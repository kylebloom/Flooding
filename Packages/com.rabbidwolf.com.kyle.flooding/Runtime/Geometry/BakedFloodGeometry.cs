using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Runtime geometry backed exclusively by immutable Editor-baked data.
    /// </summary>
    internal sealed class BakedFloodGeometry : IFloodVolumeGeometry
    {
        private static readonly int[][] Tetrahedra =
        {
            new[] { 0, 1, 3, 7 },
            new[] { 0, 3, 2, 7 },
            new[] { 0, 2, 6, 7 },
            new[] { 0, 6, 4, 7 },
            new[] { 0, 4, 5, 7 },
            new[] { 0, 5, 1, 7 },
        };

        private static readonly int[][] TetrahedronFaces =
        {
            new[] { 0, 1, 2 },
            new[] { 0, 3, 1 },
            new[] { 0, 2, 3 },
            new[] { 1, 3, 2 },
        };

        private static readonly int[,] CubeEdges =
        {
            { 0, 1 }, { 0, 2 }, { 0, 4 },
            { 1, 3 }, { 1, 5 }, { 2, 3 },
            { 2, 6 }, { 3, 7 }, { 4, 5 },
            { 4, 6 }, { 5, 7 }, { 6, 7 },
        };

        private readonly FloodVolumeData data;
        private readonly HashSet<int> occupiedCells;

        /// <summary>
        /// Creates runtime geometry from a valid baked asset.
        /// </summary>
        internal BakedFloodGeometry(FloodVolumeData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (!data.IsUsable)
                throw new ArgumentException(
                    "FloodVolumeData is missing, empty, or uses an unsupported bake format.",
                    nameof(data));

            this.data = data;
            occupiedCells = new HashSet<int>(data.OccupiedCellIndices);
        }

        /// <inheritdoc />
        public double Capacity => data.Capacity;

        /// <inheritdoc />
        public Bounds LocalBounds => data.LocalBounds;

        /// <inheritdoc />
        public FloodContainmentPrecision ContainmentPrecision =>
            FloodContainmentPrecision.BakeApproximation;

        /// <inheritdoc />
        public bool SupportsPlane(Plane localSurfacePlane)
        {
            var normal = localSurfacePlane.normal;
            return IsFinite(normal)
                && normal.sqrMagnitude
                    > FloodGeometryTolerances.PlaneNormal
                        * FloodGeometryTolerances.PlaneNormal
                && float.IsFinite(localSurfacePlane.distance);
        }

        /// <inheritdoc />
        public bool ContainsLocalPoint(Vector3 localPoint)
        {
            if (!IsFinite(localPoint)
                || !TryGetCellIndex(localPoint, out var cellIndex))
            {
                return false;
            }

            return occupiedCells.Contains(cellIndex);
        }

        private bool TryGetCellIndex(Vector3 localPoint, out int flattenedIndex)
        {
            flattenedIndex = -1;
            var bounds = data.LocalBounds;
            if (!bounds.Contains(localPoint))
                return false;

            var cellSize = data.CellSize;
            var gridSize = data.GridSize;
            var relative = localPoint - bounds.min;
            var x = Mathf.Clamp(
                Mathf.FloorToInt(relative.x / cellSize.x),
                0,
                gridSize.x - 1);
            var y = Mathf.Clamp(
                Mathf.FloorToInt(relative.y / cellSize.y),
                0,
                gridSize.y - 1);
            var z = Mathf.Clamp(
                Mathf.FloorToInt(relative.z / cellSize.z),
                0,
                gridSize.z - 1);

            flattenedIndex =
                x + (y * gridSize.x) + (z * gridSize.x * gridSize.y);
            return true;
        }

        /// <inheritdoc />
        public double CalculateSubmergedVolume(Plane localSurfacePlane)
        {
            return Evaluate(localSurfacePlane, includeSurface: false).Volume;
        }

        /// <inheritdoc />
        public FloodSubmersionResult EvaluateSubmersion(
            Plane localSurfacePlane)
        {
            return Evaluate(localSurfacePlane, includeSurface: true);
        }

        private FloodSubmersionResult Evaluate(
            Plane plane,
            bool includeSurface)
        {
            if (!SupportsPlane(plane))
                throw new ArgumentException(
                    "Surface plane must have a finite non-zero normal and finite distance.",
                    nameof(plane));

            var totalVolume = 0d;
            var weightedCentroid = Vector3.zero;
            var useBoundarySurface =
                includeSurface && data.HasPresentationBoundary;
            var voxelContours =
                includeSurface && !useBoundarySurface
                    ? new List<FloodSurfaceContour>()
                    : null;

            foreach (var cellIndex in data.OccupiedCellIndices)
            {
                var vertices = GetCellVertices(data.GetCellCenter(cellIndex));

                foreach (var tetrahedron in Tetrahedra)
                {
                    AccumulateClippedTetrahedron(
                        vertices[tetrahedron[0]],
                        vertices[tetrahedron[1]],
                        vertices[tetrahedron[2]],
                        vertices[tetrahedron[3]],
                        plane,
                        ref totalVolume,
                        ref weightedCentroid);
                }

                if (voxelContours != null
                    && TryBuildCellSurface(vertices, plane, out var contour))
                {
                    voxelContours.Add(contour);
                }
            }

            totalVolume = Math.Clamp(totalVolume, 0d, Capacity);
            var centroid =
                totalVolume > FloodGeometryTolerances.SolverAbsoluteVolume
                    ? weightedCentroid / (float)totalVolume
                    : FindMinimumBoundaryPoint(plane.normal);

            FloodSurfaceIntersection surface;
            if (useBoundarySurface)
            {
                surface = FloodMeshPlaneIntersection.IntersectMesh(
                    data.PresentationBoundaryVertices,
                    data.PresentationBoundaryTriangles,
                    plane);
            }
            else if (voxelContours != null && voxelContours.Count > 0)
            {
                surface = new FloodSurfaceIntersection(voxelContours.ToArray());
            }
            else
            {
                surface = default;
            }

            return new FloodSubmersionResult(
                totalVolume,
                centroid,
                surface);
        }

        private Vector3 FindMinimumBoundaryPoint(Vector3 normal)
        {
            normal.Normalize();
            var half = data.CellSize * 0.5f;
            var supportOffset = new Vector3(
                GetMinimumSupport(normal.x, half.x),
                GetMinimumSupport(normal.y, half.y),
                GetMinimumSupport(normal.z, half.z));
            var minimumProjection = float.PositiveInfinity;
            var accumulated = Vector3.zero;
            var count = 0;

            foreach (var cellIndex in data.OccupiedCellIndices)
            {
                var point = data.GetCellCenter(cellIndex) + supportOffset;
                var projection = Vector3.Dot(normal, point);

                if (projection
                    < minimumProjection
                        - FloodGeometryTolerances.Position)
                {
                    minimumProjection = projection;
                    accumulated = point;
                    count = 1;
                    continue;
                }

                if (Math.Abs(projection - minimumProjection)
                    <= FloodGeometryTolerances.Position)
                {
                    accumulated += point;
                    count++;
                }
            }

            return count > 0
                ? accumulated / count
                : data.Centroid;
        }

        private static float GetMinimumSupport(
            float normalComponent,
            float halfExtent)
        {
            if (Math.Abs(normalComponent)
                <= FloodGeometryTolerances.PlaneNormal)
            {
                return 0f;
            }

            return normalComponent > 0f
                ? -halfExtent
                : halfExtent;
        }

        private Vector3[] GetCellVertices(Vector3 center)
        {
            var half = data.CellSize * 0.5f;
            return new[]
            {
                center + new Vector3(-half.x, -half.y, -half.z),
                center + new Vector3(half.x, -half.y, -half.z),
                center + new Vector3(-half.x, half.y, -half.z),
                center + new Vector3(half.x, half.y, -half.z),
                center + new Vector3(-half.x, -half.y, half.z),
                center + new Vector3(half.x, -half.y, half.z),
                center + new Vector3(-half.x, half.y, half.z),
                center + new Vector3(half.x, half.y, half.z),
            };
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

            foreach (var point in tetrahedron)
            {
                if (plane.GetDistanceToPoint(point)
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
                SortOnPlane(capPoints, plane.normal);
                faces.Add(capPoints);
            }

            var uniqueVertices = new List<Vector3>(8);
            foreach (var face in faces)
            {
                foreach (var point in face)
                    AddUnique(uniqueVertices, point);
            }

            if (uniqueVertices.Count < 4)
                return;

            var interior = Vector3.zero;
            foreach (var point in uniqueVertices)
                interior += point;
            interior /= uniqueVertices.Count;

            foreach (var face in faces)
            {
                for (var index = 1; index < face.Count - 1; index++)
                {
                    AccumulateTetrahedron(
                        interior,
                        face[0],
                        face[index],
                        face[index + 1],
                        ref totalVolume,
                        ref weightedCentroid);
                }
            }
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
                    AddUnique(result, current);
                if (currentInside == nextInside)
                    continue;

                var denominator = currentDistance - nextDistance;
                if (Math.Abs(denominator)
                    <= FloodGeometryTolerances.Position)
                {
                    continue;
                }

                var intersection =
                    current + ((next - current)
                        * (currentDistance / denominator));
                AddUnique(result, intersection);
                AddUnique(capPoints, intersection);
            }

            return result;
        }

        private static void AccumulateTetrahedron(
            Vector3 first,
            Vector3 second,
            Vector3 third,
            Vector3 fourth,
            ref double totalVolume,
            ref Vector3 weightedCentroid)
        {
            var sixVolume = Vector3.Dot(
                second - first,
                Vector3.Cross(third - first, fourth - first));
            var volume = Math.Abs((double)sixVolume) / 6d;
            if (volume <= double.Epsilon)
                return;

            totalVolume += volume;
            weightedCentroid +=
                ((first + second + third + fourth) * 0.25f)
                * (float)volume;
        }

        private static bool TryBuildCellSurface(
            IReadOnlyList<Vector3> vertices,
            Plane plane,
            out FloodSurfaceContour contour)
        {
            var points = new List<Vector3>(6);
            var minimumDistance = float.PositiveInfinity;
            var maximumDistance = float.NegativeInfinity;

            foreach (var vertex in vertices)
            {
                var distance = plane.GetDistanceToPoint(vertex);
                minimumDistance = Math.Min(minimumDistance, distance);
                maximumDistance = Math.Max(maximumDistance, distance);
            }

            if (minimumDistance
                    >= -FloodGeometryTolerances.Position
                || maximumDistance
                    < -FloodGeometryTolerances.Position)
            {
                contour = default;
                return false;
            }

            for (var edge = 0; edge < CubeEdges.GetLength(0); edge++)
            {
                var first = vertices[CubeEdges[edge, 0]];
                var second = vertices[CubeEdges[edge, 1]];
                var firstDistance = plane.GetDistanceToPoint(first);
                var secondDistance = plane.GetDistanceToPoint(second);

                if (Math.Abs(firstDistance)
                    <= FloodGeometryTolerances.Position)
                {
                    AddUnique(points, first);
                }

                if (Math.Abs(secondDistance)
                    <= FloodGeometryTolerances.Position)
                {
                    AddUnique(points, second);
                }

                if ((firstDistance < -FloodGeometryTolerances.Position
                        && secondDistance
                            > FloodGeometryTolerances.Position)
                    || (firstDistance > FloodGeometryTolerances.Position
                        && secondDistance
                            < -FloodGeometryTolerances.Position))
                {
                    AddUnique(
                        points,
                        first + ((second - first)
                            * (firstDistance
                                / (firstDistance - secondDistance))));
                }
            }

            if (points.Count < 3)
            {
                contour = default;
                return false;
            }

            SortOnPlane(points, plane.normal);
            contour = new FloodSurfaceContour(points.ToArray());
            return true;
        }

        private static void SortOnPlane(
            List<Vector3> points,
            Vector3 normal)
        {
            var center = Vector3.zero;
            foreach (var point in points)
                center += point;
            center /= points.Count;

            normal.Normalize();
            var tangent = Math.Abs(normal.y) < 0.9f
                ? Vector3.Cross(normal, Vector3.up).normalized
                : Vector3.Cross(normal, Vector3.right).normalized;
            var bitangent = Vector3.Cross(normal, tangent).normalized;

            points.Sort(
                (first, second) =>
                    Math.Atan2(
                        Vector3.Dot(first - center, bitangent),
                        Vector3.Dot(first - center, tangent))
                    .CompareTo(
                        Math.Atan2(
                            Vector3.Dot(second - center, bitangent),
                            Vector3.Dot(second - center, tangent))));
        }

        private static void AddUnique(
            List<Vector3> points,
            Vector3 candidate)
        {
            foreach (var point in points)
            {
                if ((point - candidate).sqrMagnitude
                    <= FloodGeometryTolerances.Position
                        * FloodGeometryTolerances.Position)
                {
                    return;
                }
            }

            points.Add(candidate);
        }

        private static bool IsFinite(Vector3 value)
        {
            return float.IsFinite(value.x)
                && float.IsFinite(value.y)
                && float.IsFinite(value.z);
        }
    }
}
