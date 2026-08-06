using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Ear-clips simple planar polygons for free-surface mesh generation.
    /// Hole loops (parent/child rings) are not supported — each contour is
    /// triangulated independently.
    /// </summary>
    internal static class FloodPlanarPolygonTriangulation
    {
        /// <summary>
        /// Appends contour vertices and ear-clipped triangles into the mesh
        /// buffers. Falls back to a triangle fan if ear clipping fails so
        /// convex voxel patches still draw.
        /// </summary>
        public static void AppendContour(
            IReadOnlyList<Vector3> contour,
            Vector3 planeNormal,
            List<Vector3> vertices,
            List<int> triangles)
        {
            if (contour == null || contour.Count < 3)
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

            var indices = Triangulate(projected);
            if (indices.Length == 0)
            {
                // Opposite winding (e.g. unexpected contour orientation).
                for (var index = 0; index < projected.Length; index++)
                    projected[index] = new Vector2(
                        projected[index].x,
                        -projected[index].y);
                indices = Triangulate(projected);
            }

            var offset = vertices.Count;
            for (var index = 0; index < contour.Count; index++)
                vertices.Add(contour[index]);

            if (indices.Length >= 3)
            {
                for (var index = 0; index < indices.Length; index++)
                    triangles.Add(offset + indices[index]);
                return;
            }

            // Last-resort fan for convex-ish contours when ear clipping fails.
            for (var index = 1; index < contour.Count - 1; index++)
            {
                triangles.Add(offset);
                triangles.Add(offset + index);
                triangles.Add(offset + index + 1);
            }
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
    }
}
