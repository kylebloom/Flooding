using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Deterministic simple polygon footprint extruded along local Y.
    /// Concave footprints are supported; holes and self-intersections are not.
    /// </summary>
    public sealed class ExtrudedPolygonFloodGeometry :
        ExtrudedFloodVolumeGeometry
    {
        /// <summary>
        /// Creates geometry from one simple polygon footprint.
        /// Clockwise footprints are accepted and normalized.
        /// </summary>
        public ExtrudedPolygonFloodGeometry(
            IReadOnlyList<Vector2> footprint,
            double maximumHeight)
            : this(Prepare(footprint, maximumHeight), maximumHeight)
        {
        }

        private ExtrudedPolygonFloodGeometry(
            PreparedPolygon prepared,
            double maximumHeight)
            : base(
                prepared.Vertices,
                prepared.Triangles,
                prepared.Area,
                maximumHeight,
                prepared.Centroid,
                prepared.Bounds)
        {
        }

        private static PreparedPolygon Prepare(
            IReadOnlyList<Vector2> footprint,
            double maximumHeight)
        {
            if (double.IsNaN(maximumHeight)
                || double.IsInfinity(maximumHeight)
                || maximumHeight
                    < FloodGeometryTolerances.MinimumDimension)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumHeight),
                    maximumHeight,
                    $"Maximum height must be finite and at least "
                    + $"{FloodGeometryTolerances.MinimumDimension} meters.");
            }

            if (!FloodPolygonValidation.TryValidate(
                    footprint,
                    out var winding,
                    out var message))
            {
                throw new ArgumentException(message, nameof(footprint));
            }

            var normalized = new List<Vector2>(footprint.Count);

            if (winding == FloodPolygonWinding.CounterClockwise)
            {
                for (var index = 0; index < footprint.Count; index++)
                    normalized.Add(footprint[index]);
            }
            else
            {
                for (var index = footprint.Count - 1; index >= 0; index--)
                    normalized.Add(footprint[index]);
            }

            RemoveRedundantCollinearVertices(normalized);

            if (normalized.Count < 3)
            {
                throw new ArgumentException(
                    "Polygon footprint has fewer than three non-collinear points.",
                    nameof(footprint));
            }

            var area = FloodPolygonValidation.CalculateSignedArea(normalized);
            var centroid = CalculateCentroid(normalized, area);
            var triangles = Triangulate(normalized);
            var bounds = CalculateBounds(normalized, maximumHeight);

            return new PreparedPolygon(
                normalized.ToArray(),
                triangles,
                area,
                centroid,
                bounds);
        }

        private static void RemoveRedundantCollinearVertices(
            List<Vector2> vertices)
        {
            var removedPoint = true;

            while (removedPoint && vertices.Count > 3)
            {
                removedPoint = false;

                for (var index = 0; index < vertices.Count; index++)
                {
                    var previous =
                        vertices[(index - 1 + vertices.Count) % vertices.Count];
                    var current = vertices[index];
                    var next = vertices[(index + 1) % vertices.Count];
                    var cross = Cross(previous, current, next);

                    if (Math.Abs(cross)
                        > FloodGeometryTolerances.Position)
                    {
                        continue;
                    }

                    vertices.RemoveAt(index);
                    removedPoint = true;
                    break;
                }
            }
        }

        private static Vector2 CalculateCentroid(
            IReadOnlyList<Vector2> vertices,
            double area)
        {
            var weightedX = 0d;
            var weightedY = 0d;

            for (var index = 0; index < vertices.Count; index++)
            {
                var next = (index + 1) % vertices.Count;
                var cross =
                    ((double)vertices[index].x * vertices[next].y)
                    - ((double)vertices[next].x * vertices[index].y);

                weightedX += (vertices[index].x + vertices[next].x) * cross;
                weightedY += (vertices[index].y + vertices[next].y) * cross;
            }

            var divisor = 6d * area;
            return new Vector2(
                (float)(weightedX / divisor),
                (float)(weightedY / divisor));
        }

        private static int[] Triangulate(IReadOnlyList<Vector2> vertices)
        {
            var remaining = new List<int>(vertices.Count);
            var triangles = new List<int>((vertices.Count - 2) * 3);

            for (var index = 0; index < vertices.Count; index++)
                remaining.Add(index);

            var maximumAttempts = vertices.Count * vertices.Count;
            var attempts = 0;

            while (remaining.Count > 3 && attempts < maximumAttempts)
            {
                var clippedEar = false;

                for (var index = 0; index < remaining.Count; index++)
                {
                    var previous =
                        remaining[(index - 1 + remaining.Count)
                            % remaining.Count];
                    var current = remaining[index];
                    var next = remaining[(index + 1) % remaining.Count];

                    if (Cross(
                            vertices[previous],
                            vertices[current],
                            vertices[next])
                        <= FloodGeometryTolerances.Position)
                    {
                        continue;
                    }

                    if (ContainsOtherVertex(
                            vertices,
                            remaining,
                            previous,
                            current,
                            next))
                    {
                        continue;
                    }

                    triangles.Add(previous);
                    triangles.Add(current);
                    triangles.Add(next);
                    remaining.RemoveAt(index);
                    clippedEar = true;
                    break;
                }

                if (!clippedEar)
                    break;

                attempts++;
            }

            if (remaining.Count != 3)
            {
                throw new ArgumentException(
                    "Polygon footprint could not be triangulated. Remove "
                    + "nearly overlapping or extremely narrow edges.");
            }

            triangles.Add(remaining[0]);
            triangles.Add(remaining[1]);
            triangles.Add(remaining[2]);
            return triangles.ToArray();
        }

        private static bool ContainsOtherVertex(
            IReadOnlyList<Vector2> vertices,
            IReadOnlyList<int> remaining,
            int first,
            int second,
            int third)
        {
            for (var index = 0; index < remaining.Count; index++)
            {
                var candidate = remaining[index];

                if (candidate == first
                    || candidate == second
                    || candidate == third)
                {
                    continue;
                }

                if (IsPointInTriangle(
                        vertices[candidate],
                        vertices[first],
                        vertices[second],
                        vertices[third]))
                {
                    return true;
                }
            }

            return false;
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

        private static Bounds CalculateBounds(
            IReadOnlyList<Vector2> vertices,
            double maximumHeight)
        {
            var minimumX = vertices[0].x;
            var maximumX = vertices[0].x;
            var minimumZ = vertices[0].y;
            var maximumZ = vertices[0].y;

            for (var index = 1; index < vertices.Count; index++)
            {
                minimumX = Math.Min(minimumX, vertices[index].x);
                maximumX = Math.Max(maximumX, vertices[index].x);
                minimumZ = Math.Min(minimumZ, vertices[index].y);
                maximumZ = Math.Max(maximumZ, vertices[index].y);
            }

            return new Bounds(
                new Vector3(
                    (minimumX + maximumX) * 0.5f,
                    (float)(maximumHeight * 0.5d),
                    (minimumZ + maximumZ) * 0.5f),
                new Vector3(
                    maximumX - minimumX,
                    (float)maximumHeight,
                    maximumZ - minimumZ));
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

        private sealed class PreparedPolygon
        {
            public PreparedPolygon(
                Vector2[] vertices,
                int[] triangles,
                double area,
                Vector2 centroid,
                Bounds bounds)
            {
                Vertices = vertices;
                Triangles = triangles;
                Area = area;
                Centroid = centroid;
                Bounds = bounds;
            }

            public Vector2[] Vertices { get; }
            public int[] Triangles { get; }
            public double Area { get; }
            public Vector2 Centroid { get; }
            public Bounds Bounds { get; }
        }
    }
}
