using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Prototype region-union strategy for exactly two rectangular members that
    /// are axis-aligned in region-local space.
    /// </summary>
    /// <remarks>
    /// Uses exact inclusion-exclusion for capacity and volume-below-plane.
    /// Kept for eligible two rectangular members when no
    /// <see cref="FloodRegionData"/> bake is assigned. For N members or mixed
    /// geometry modes, bake a region occupancy asset and use
    /// <see cref="RegionOccupancyUnionStrategy"/>.
    /// </remarks>
    public sealed class TwoBoxAnalyticUnionStrategy : IRegionUnionStrategy
    {
        /// <summary>
        /// Returns whether the member list is eligible for this strategy.
        /// </summary>
        public static bool CanHandle(IReadOnlyList<FloodVolume> members)
        {
            return members != null
                && members.Count == 2
                && members[0] != null
                && members[1] != null
                && members[0].GeometryMode == FloodGeometryMode.RectangularPrism
                && members[1].GeometryMode == FloodGeometryMode.RectangularPrism;
        }

        /// <inheritdoc />
        public bool TryBuild(
            Transform regionTransform,
            IReadOnlyList<FloodVolume> members,
            out IFloodVolumeGeometry geometry,
            out string message)
        {
            geometry = null;
            message = null;

            if (!CanHandle(members))
            {
                message =
                    "TwoBoxAnalyticUnionStrategy requires exactly two "
                    + "Rectangular Prism FloodVolume members.";
                return false;
            }

            if (regionTransform == null)
            {
                message = "Region transform is required.";
                return false;
            }

            var volumeA = members[0];
            var volumeB = members[1];

            if (!TryGetRegionLocalBounds(
                    regionTransform,
                    volumeA,
                    out var boundsA,
                    out message)
                || !TryGetRegionLocalBounds(
                    regionTransform,
                    volumeB,
                    out var boundsB,
                    out message))
            {
                return false;
            }

            if (!TryClassifyContinuity(
                    boundsA,
                    boundsB,
                    out var intersection,
                    out var continuity,
                    out message))
            {
                return false;
            }

            if (continuity == ContinuityKind.Disconnected)
            {
                message =
                    $"FloodVolumes '{volumeA.name}' and '{volumeB.name}' are "
                    + "disconnected in region space. Members must overlap or "
                    + "share a face within tolerance.";
                return false;
            }

            IFloodVolumeGeometry intersectionGeometry = null;
            if (continuity == ContinuityKind.Overlapping
                && intersection.size.x > FloodGeometryTolerances.MinimumDimension
                && intersection.size.y > FloodGeometryTolerances.MinimumDimension
                && intersection.size.z > FloodGeometryTolerances.MinimumDimension)
            {
                intersectionGeometry = AxisAlignedBoxFloodGeometry.Create(
                    intersection);
            }

            var boxA = AxisAlignedBoxFloodGeometry.Create(boundsA);
            var boxB = AxisAlignedBoxFloodGeometry.Create(boundsB);

            IExtrudedFloodVolumeGeometry presentationGeometry = null;
            if (TryBuildEqualHeightUnionPresentation(
                    boundsA,
                    boundsB,
                    intersection,
                    continuity,
                    out var extruded,
                    out _))
            {
                presentationGeometry = extruded;
            }

            geometry = new TwoBoxInclusionExclusionGeometry(
                boxA,
                boxB,
                intersectionGeometry,
                presentationGeometry);
            message = string.Empty;
            return true;
        }

        private static bool TryGetRegionLocalBounds(
            Transform regionTransform,
            FloodVolume volume,
            out Bounds regionLocalBounds,
            out string message)
        {
            regionLocalBounds = default;
            message = null;

            var memberGeometry = volume.Geometry;
            if (memberGeometry == null)
            {
                message = $"FloodVolume '{volume.name}' has invalid geometry.";
                return false;
            }

            if (!IsAxisAlignedWithRegion(regionTransform, volume.transform))
            {
                message =
                    $"FloodVolume '{volume.name}' must be axis-aligned with the "
                    + "FloodRegion transform for TwoBoxAnalyticUnionStrategy.";
                return false;
            }

            var localBounds = memberGeometry.LocalBounds;
            var min = new Vector3(
                float.PositiveInfinity,
                float.PositiveInfinity,
                float.PositiveInfinity);
            var max = new Vector3(
                float.NegativeInfinity,
                float.NegativeInfinity,
                float.NegativeInfinity);

            for (var x = 0; x < 2; x++)
            {
                for (var y = 0; y < 2; y++)
                {
                    for (var z = 0; z < 2; z++)
                    {
                        var corner = new Vector3(
                            x == 0 ? localBounds.min.x : localBounds.max.x,
                            y == 0 ? localBounds.min.y : localBounds.max.y,
                            z == 0 ? localBounds.min.z : localBounds.max.z);
                        var regionCorner = regionTransform.InverseTransformPoint(
                            volume.transform.TransformPoint(corner));
                        min = Vector3.Min(min, regionCorner);
                        max = Vector3.Max(max, regionCorner);
                    }
                }
            }

            regionLocalBounds = new Bounds();
            regionLocalBounds.SetMinMax(min, max);
            message = string.Empty;
            return true;
        }

        private static bool IsAxisAlignedWithRegion(
            Transform regionTransform,
            Transform memberTransform)
        {
            var relative = Quaternion.Inverse(regionTransform.rotation)
                * memberTransform.rotation;
            var euler = relative.eulerAngles;
            return IsRightAngle(euler.x)
                && IsRightAngle(euler.y)
                && IsRightAngle(euler.z);
        }

        private static bool IsRightAngle(float degrees)
        {
            var wrapped = Mathf.Abs(Mathf.DeltaAngle(0f, degrees));
            return wrapped <= 0.1f
                || Mathf.Abs(wrapped - 90f) <= 0.1f
                || Mathf.Abs(wrapped - 180f) <= 0.1f
                || Mathf.Abs(wrapped - 270f) <= 0.1f;
        }

        private static bool TryClassifyContinuity(
            Bounds boundsA,
            Bounds boundsB,
            out Bounds intersection,
            out ContinuityKind continuity,
            out string message)
        {
            message = string.Empty;
            intersection = default;
            continuity = ContinuityKind.Disconnected;

            var min = Vector3.Max(boundsA.min, boundsB.min);
            var max = Vector3.Min(boundsA.max, boundsB.max);
            var size = max - min;

            var epsilon = (float)FloodGeometryTolerances.Position;
            var overlapX = size.x > epsilon;
            var overlapY = size.y > epsilon;
            var overlapZ = size.z > epsilon;

            if (overlapX && overlapY && overlapZ)
            {
                intersection.SetMinMax(min, max);
                continuity = ContinuityKind.Overlapping;
                return true;
            }

            // Face-sharing / edge contact within tolerance: expanded overlap.
            var touchEpsilon = Mathf.Max(
                epsilon,
                (float)FloodGeometryTolerances.MinimumDimension * 0.1f);
            var expandedA = boundsA;
            expandedA.Expand(touchEpsilon * 2f);

            if (!expandedA.Intersects(boundsB))
            {
                continuity = ContinuityKind.Disconnected;
                return true;
            }

            // Degenerate intersection (zero-volume contact).
            intersection.SetMinMax(
                Vector3.Min(min, max),
                Vector3.Max(min, max));
            continuity = ContinuityKind.Touching;
            return true;
        }

        private static bool TryBuildEqualHeightUnionPresentation(
            Bounds boundsA,
            Bounds boundsB,
            Bounds intersection,
            ContinuityKind continuity,
            out ExtrudedPolygonFloodGeometry geometry,
            out string message)
        {
            geometry = null;
            message = null;

            var heightEpsilon = (float)FloodGeometryTolerances.Position;
            if (Mathf.Abs(boundsA.min.y - boundsB.min.y) > heightEpsilon
                || Mathf.Abs(boundsA.max.y - boundsB.max.y) > heightEpsilon)
            {
                message = "Unequal height ranges; presentation union unavailable.";
                return false;
            }

            var height = boundsA.max.y - boundsA.min.y;
            if (height < (float)FloodGeometryTolerances.MinimumDimension)
            {
                message = "Degenerate height.";
                return false;
            }

            if (Mathf.Abs(boundsA.min.y) > heightEpsilon)
            {
                message =
                    "Presentation union currently requires floor at region Y=0.";
                return false;
            }

            if (!TryBuildAxisAlignedRectUnionFootprint(
                    boundsA,
                    boundsB,
                    out var footprint))
            {
                message = "Failed to build union footprint.";
                return false;
            }

            try
            {
                geometry = new ExtrudedPolygonFloodGeometry(footprint, height);
                message = string.Empty;
                return true;
            }
            catch (ArgumentException exception)
            {
                message = exception.Message;
                return false;
            }
        }

        private static bool TryBuildAxisAlignedRectUnionFootprint(
            Bounds boundsA,
            Bounds boundsB,
            out Vector2[] footprint)
        {
            footprint = null;

            var a = ToXzRect(boundsA);
            var b = ToXzRect(boundsB);

            // One contains the other.
            if (ContainsRect(a, b))
            {
                footprint = RectFootprint(a);
                return true;
            }

            if (ContainsRect(b, a))
            {
                footprint = RectFootprint(b);
                return true;
            }

            // Disjoint in XZ — only valid when 3D continuity was face-touch along XZ edge.
            if (!RectsOverlapOrTouch(a, b))
            {
                return false;
            }

            // Same full span in one axis → merged rectangle.
            if (Approximately(a.yMin, b.yMin) && Approximately(a.yMax, b.yMax))
            {
                footprint = RectFootprint(
                    new Rect(
                        Mathf.Min(a.xMin, b.xMin),
                        a.yMin,
                        Mathf.Max(a.xMax, b.xMax) - Mathf.Min(a.xMin, b.xMin),
                        a.height));
                return true;
            }

            if (Approximately(a.xMin, b.xMin) && Approximately(a.xMax, b.xMax))
            {
                footprint = RectFootprint(
                    new Rect(
                        a.xMin,
                        Mathf.Min(a.yMin, b.yMin),
                        a.width,
                        Mathf.Max(a.yMax, b.yMax) - Mathf.Min(a.yMin, b.yMin)));
                return true;
            }

            // L-shaped (or more general) rectilinear union via cell marking.
            return TryBuildRectilinearUnionFootprint(a, b, out footprint);
        }

        private static bool TryBuildRectilinearUnionFootprint(
            Rect a,
            Rect b,
            out Vector2[] footprint)
        {
            footprint = null;
            var xs = SortedUnique(a.xMin, a.xMax, b.xMin, b.xMax);
            var zs = SortedUnique(a.yMin, a.yMax, b.yMin, b.yMax);

            if (xs.Count < 2 || zs.Count < 2)
                return false;

            var cols = xs.Count - 1;
            var rows = zs.Count - 1;
            var filled = new bool[cols, rows];

            for (var x = 0; x < cols; x++)
            {
                for (var z = 0; z < rows; z++)
                {
                    var cx = (xs[x] + xs[x + 1]) * 0.5f;
                    var cz = (zs[z] + zs[z + 1]) * 0.5f;
                    filled[x, z] = a.Contains(new Vector2(cx, cz))
                        || b.Contains(new Vector2(cx, cz));
                }
            }

            var hasFilled = false;
            for (var x = 0; x < cols && !hasFilled; x++)
            {
                for (var z = 0; z < rows; z++)
                {
                    if (!filled[x, z])
                        continue;

                    hasFilled = true;
                    break;
                }
            }

            if (!hasFilled)
                return false;

            var points = new List<Vector2>();
            TraceRectilinearBoundary(filled, xs, zs, points);

            if (points.Count < 3)
                return false;

            footprint = points.ToArray();
            return true;
        }

        private static void TraceRectilinearBoundary(
            bool[,] filled,
            List<float> xs,
            List<float> zs,
            List<Vector2> points)
        {
            // Collect unique boundary corners of filled cells (outer corners).
            var cornerSet = new HashSet<Vector2Int>();
            var cols = filled.GetLength(0);
            var rows = filled.GetLength(1);

            for (var x = 0; x < cols; x++)
            {
                for (var z = 0; z < rows; z++)
                {
                    if (!filled[x, z])
                        continue;

                    cornerSet.Add(new Vector2Int(x, z));
                    cornerSet.Add(new Vector2Int(x + 1, z));
                    cornerSet.Add(new Vector2Int(x, z + 1));
                    cornerSet.Add(new Vector2Int(x + 1, z + 1));
                }
            }

            // Keep only silhouette corners: not completely surrounded as interior.
            var silhouette = new List<Vector2Int>();
            foreach (var corner in cornerSet)
            {
                var occupiedNeighbors = CountFilledAroundCorner(
                    filled,
                    corner.x,
                    corner.y);
                if (occupiedNeighbors > 0 && occupiedNeighbors < 4)
                    silhouette.Add(corner);
            }

            if (silhouette.Count < 3)
                return;

            // Order silhouette CCW by angle around centroid.
            var centroid = Vector2.zero;
            foreach (var corner in silhouette)
            {
                centroid += new Vector2(xs[corner.x], zs[corner.y]);
            }

            centroid /= silhouette.Count;
            silhouette.Sort((left, right) =>
            {
                var leftAngle = Mathf.Atan2(
                    zs[left.y] - centroid.y,
                    xs[left.x] - centroid.x);
                var rightAngle = Mathf.Atan2(
                    zs[right.y] - centroid.y,
                    xs[right.x] - centroid.x);
                return leftAngle.CompareTo(rightAngle);
            });

            foreach (var corner in silhouette)
                points.Add(new Vector2(xs[corner.x], zs[corner.y]));
        }

        private static int CountFilledAroundCorner(
            bool[,] filled,
            int cornerX,
            int cornerY)
        {
            var cols = filled.GetLength(0);
            var rows = filled.GetLength(1);
            var count = 0;

            for (var dx = -1; dx <= 0; dx++)
            {
                for (var dz = -1; dz <= 0; dz++)
                {
                    var x = cornerX + dx;
                    var z = cornerY + dz;
                    if (x < 0 || z < 0 || x >= cols || z >= rows)
                        continue;
                    if (filled[x, z])
                        count++;
                }
            }

            return count;
        }

        private static Rect ToXzRect(Bounds bounds)
        {
            return Rect.MinMaxRect(
                bounds.min.x,
                bounds.min.z,
                bounds.max.x,
                bounds.max.z);
        }

        private static Vector2[] RectFootprint(Rect rect)
        {
            return new[]
            {
                new Vector2(rect.xMin, rect.yMin),
                new Vector2(rect.xMax, rect.yMin),
                new Vector2(rect.xMax, rect.yMax),
                new Vector2(rect.xMin, rect.yMax),
            };
        }

        private static bool ContainsRect(Rect outer, Rect inner)
        {
            return outer.xMin <= inner.xMin + 1e-5f
                && outer.xMax >= inner.xMax - 1e-5f
                && outer.yMin <= inner.yMin + 1e-5f
                && outer.yMax >= inner.yMax - 1e-5f;
        }

        private static bool RectsOverlapOrTouch(Rect a, Rect b)
        {
            return a.xMin <= b.xMax + 1e-5f
                && a.xMax >= b.xMin - 1e-5f
                && a.yMin <= b.yMax + 1e-5f
                && a.yMax >= b.yMin - 1e-5f;
        }

        private static bool Approximately(float left, float right)
        {
            return Mathf.Abs(left - right)
                <= (float)FloodGeometryTolerances.Position;
        }

        private static List<float> SortedUnique(params float[] values)
        {
            var list = new List<float>(values);
            list.Sort();
            for (var index = list.Count - 1; index > 0; index--)
            {
                if (Mathf.Abs(list[index] - list[index - 1])
                    <= (float)FloodGeometryTolerances.Position)
                {
                    list.RemoveAt(index);
                }
            }

            return list;
        }

        private enum ContinuityKind
        {
            Disconnected = 0,
            Touching = 1,
            Overlapping = 2,
        }
    }
}
