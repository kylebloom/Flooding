using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Kyle.Flooding;
using UnityEditor;
using UnityEngine;

namespace Kyle.Flooding.Editor
{
    /// <summary>
    /// Editor-only region-local occupancy bake for <see cref="FloodRegion"/>.
    /// Samples all members via containment into the region grid, and also
    /// remaps baked member cell centers so thin features are retained.
    /// Never runs at runtime.
    /// </summary>
    internal static class FloodRegionBaker
    {
        /// <summary>
        /// Bakes the region's members into a <see cref="FloodRegionData"/> asset.
        /// </summary>
        public static bool TryBake(
            FloodRegion region,
            out FloodRegionData data,
            out string message,
            bool promptForAssetPath = true)
        {
            data = null;
            if (!TryValidateBakeInputs(region, out message))
                return false;

            if (!TryComputeRegionBounds(
                    region,
                    out var bounds,
                    out message))
            {
                return false;
            }

            if (!IsFinitePositive(bounds.size.x)
                || !IsFinitePositive(bounds.size.y)
                || !IsFinitePositive(bounds.size.z))
            {
                message =
                    "Member geometry produces zero or non-finite region-local "
                    + "bounds. Check member transforms and sizes.";
                return false;
            }

            var gridSize = new Vector3Int(
                Mathf.Max(
                    1,
                    Mathf.CeilToInt(bounds.size.x / region.CellResolution)),
                Mathf.Max(
                    1,
                    Mathf.CeilToInt(bounds.size.y / region.CellResolution)),
                Mathf.Max(
                    1,
                    Mathf.CeilToInt(bounds.size.z / region.CellResolution)));
            var totalGridCells =
                (long)gridSize.x * gridSize.y * gridSize.z;

            if (totalGridCells > region.MaximumGridCells)
            {
                message =
                    $"Bake requires {totalGridCells:N0} grid cells, exceeding "
                    + $"the configured limit of {region.MaximumGridCells:N0}. "
                    + "Increase Cell Resolution or the safety limit.";
                return false;
            }

            var cellSize = new Vector3(
                bounds.size.x / gridSize.x,
                bounds.size.y / gridSize.y,
                bounds.size.z / gridSize.z);

            var occupiedSet = new HashSet<int>();
            var boundaryCellCount = 0;

            // Sample every member (including baked) through containment so
            // mixed-mode unions stay face-connected on the region grid.
            SampleMembers(
                region,
                bounds,
                cellSize,
                gridSize,
                occupiedSet,
                ref boundaryCellCount);

            // Remap baked cell centers so thin baked features still contribute
            // when a region cell center misses them.
            RemapBakedMembers(
                region,
                bounds,
                cellSize,
                gridSize,
                occupiedSet);

            if (occupiedSet.Count == 0)
            {
                message =
                    "The selected resolution produced no occupied cells. "
                    + "Use a smaller Cell Resolution or check member geometry.";
                return false;
            }

            if (!IsFaceConnected(occupiedSet, gridSize))
            {
                message =
                    "Baked members are disconnected in region space. Members "
                    + "must overlap or share a face within the region grid "
                    + "(face-adjacent occupied cells).";
                return false;
            }

            var occupied = new int[occupiedSet.Count];
            occupiedSet.CopyTo(occupied);
            Array.Sort(occupied);

            var fingerprint = CreateFingerprint(region);
            data = region.BakedRegionData;

            if (data == null)
            {
                if (!promptForAssetPath)
                {
                    data = ScriptableObject.CreateInstance<FloodRegionData>();
                }
                else
                {
                    var path = EditorUtility.SaveFilePanelInProject(
                        "Save Flood Region Data",
                        $"{region.name} FloodRegionData",
                        "asset",
                        "Choose a project asset path for the immutable region bake.");
                    if (string.IsNullOrEmpty(path))
                    {
                        message = "Bake cancelled before creating an asset.";
                        return false;
                    }

                    data = ScriptableObject.CreateInstance<FloodRegionData>();
                    AssetDatabase.CreateAsset(data, path);
                }
            }

            if (promptForAssetPath)
                Undo.RecordObject(data, "Bake Flood Region Data");

            data.Initialize(
                bounds,
                cellSize,
                gridSize,
                occupied,
                boundaryCellCount,
                fingerprint);

            if (promptForAssetPath)
            {
                EditorUtility.SetDirty(data);
                Undo.RecordObject(region, "Assign Flood Region Bake");
            }

            region.AssignBake(data);

            if (promptForAssetPath)
            {
                EditorUtility.SetDirty(region);
                AssetDatabase.SaveAssets();
            }

            message =
                $"Baked {occupied.Length:N0} occupied region cells "
                + $"({data.Capacity:0.###} m³) from "
                + $"{totalGridCells:N0} inspected cells across "
                + $"{region.Members.Count} members. Presentation uses voxel "
                + "free-surface fallback (no merged presentation boundary).";
            return true;
        }

        /// <summary>
        /// Reports whether the assigned bake matches current members and settings.
        /// </summary>
        public static bool TryGetStatus(
            FloodRegion region,
            out bool stale,
            out string message)
        {
            stale = false;

            if (region == null)
            {
                message = "FloodRegion is required.";
                return false;
            }

            if (!region.TryValidateMembers(out message))
                return false;

            if (region.Members.Count < 2)
            {
                message =
                    "One-member regions use the member geometry directly; "
                    + "region bake is optional.";
                return true;
            }

            if (region.BakedRegionData == null)
            {
                if (region.Members.Count == 2
                    && TwoBoxAnalyticUnionStrategy.CanHandle(region.Members))
                {
                    message =
                        "No FloodRegionData assigned. Eligible two rectangular "
                        + "members will use TwoBoxAnalyticUnionStrategy. Bake "
                        + "Region for mixed modes or to lock an occupancy union.";
                    return true;
                }

                message =
                    "No Flood Region Data asset is assigned. Bake Region is "
                    + "required for these members.";
                return false;
            }

            if (!region.BakedRegionData.IsUsable)
            {
                message =
                    "Assigned bake is empty or uses an unsupported format. "
                    + "Re-bake required.";
                return false;
            }

            var currentFingerprint = CreateFingerprint(region);
            stale = !string.Equals(
                currentFingerprint,
                region.BakedRegionData.SourceFingerprint,
                StringComparison.Ordinal);

            if (stale)
            {
                message =
                    "Bake is stale: members, member transforms/geometry, or "
                    + "Cell Resolution changed since the last bake.";
                return true;
            }

            message = "Bake is current.";
            return true;
        }

        private static bool TryValidateBakeInputs(
            FloodRegion region,
            out string message)
        {
            if (region == null)
            {
                message = "FloodRegion is required.";
                return false;
            }

            if (!region.TryValidateMembers(out message))
                return false;

            if (region.Members.Count < 2)
            {
                message =
                    "Bake Region requires at least two FloodVolume members.";
                return false;
            }

            for (var index = 0; index < region.Members.Count; index++)
            {
                var member = region.Members[index];
                if (member.Geometry == null)
                {
                    message =
                        $"FloodVolume '{member.name}' has invalid geometry.";
                    return false;
                }

                if (member.GeometryMode == FloodGeometryMode.BakedData
                    && (member.BakedVolumeData == null
                        || !member.BakedVolumeData.IsUsable))
                {
                    message =
                        $"FloodVolume '{member.name}' is in Baked Data mode "
                        + "but has no usable FloodVolumeData.";
                    return false;
                }
            }

            message = string.Empty;
            return true;
        }

        private static bool TryComputeRegionBounds(
            FloodRegion region,
            out Bounds bounds,
            out string message)
        {
            bounds = default;
            message = null;
            var hasBounds = false;
            var min = new Vector3(
                float.PositiveInfinity,
                float.PositiveInfinity,
                float.PositiveInfinity);
            var max = new Vector3(
                float.NegativeInfinity,
                float.NegativeInfinity,
                float.NegativeInfinity);

            for (var index = 0; index < region.Members.Count; index++)
            {
                var member = region.Members[index];
                var localBounds = member.Geometry.LocalBounds;

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
                            var regionCorner =
                                region.transform.InverseTransformPoint(
                                    member.transform.TransformPoint(corner));
                            min = Vector3.Min(min, regionCorner);
                            max = Vector3.Max(max, regionCorner);
                            hasBounds = true;
                        }
                    }
                }
            }

            if (!hasBounds)
            {
                message = "Unable to compute region-local member bounds.";
                return false;
            }

            // Pad slightly so face-touching cells near bounds edges are retained.
            var pad = Mathf.Max(
                region.CellResolution * 0.01f,
                (float)FloodGeometryTolerances.Position);
            min -= Vector3.one * pad;
            max += Vector3.one * pad;
            bounds.SetMinMax(min, max);
            message = string.Empty;
            return true;
        }

        private static void SampleMembers(
            FloodRegion region,
            Bounds bounds,
            Vector3 cellSize,
            Vector3Int gridSize,
            HashSet<int> occupiedSet,
            ref int boundaryCellCount)
        {
            var flattenedIndex = 0;

            for (var z = 0; z < gridSize.z; z++)
            {
                for (var y = 0; y < gridSize.y; y++)
                {
                    for (var x = 0; x < gridSize.x; x++)
                    {
                        var minimum = bounds.min + new Vector3(
                            x * cellSize.x,
                            y * cellSize.y,
                            z * cellSize.z);
                        var center = minimum + (cellSize * 0.5f);
                        var centerInside = IsInsideAnyMember(
                            region,
                            center);
                        var hasInsideCorner = false;
                        var hasOutsideCorner = false;

                        for (var corner = 0; corner < 8; corner++)
                        {
                            var point = minimum + new Vector3(
                                (corner & 1) == 0 ? 0f : cellSize.x,
                                (corner & 2) == 0 ? 0f : cellSize.y,
                                (corner & 4) == 0 ? 0f : cellSize.z);
                            if (IsInsideAnyMember(region, point))
                                hasInsideCorner = true;
                            else
                                hasOutsideCorner = true;
                        }

                        if (hasInsideCorner && hasOutsideCorner)
                            boundaryCellCount++;
                        if (centerInside)
                            occupiedSet.Add(flattenedIndex);

                        flattenedIndex++;
                    }
                }
            }
        }

        private static void RemapBakedMembers(
            FloodRegion region,
            Bounds bounds,
            Vector3 cellSize,
            Vector3Int gridSize,
            HashSet<int> occupiedSet)
        {
            for (var index = 0; index < region.Members.Count; index++)
            {
                var member = region.Members[index];
                if (member.GeometryMode != FloodGeometryMode.BakedData
                    || member.BakedVolumeData == null
                    || !member.BakedVolumeData.IsUsable)
                {
                    continue;
                }

                var volumeData = member.BakedVolumeData;
                foreach (var sourceIndex in volumeData.OccupiedCellIndices)
                {
                    var memberLocal = volumeData.GetCellCenter(sourceIndex);
                    var world = member.transform.TransformPoint(memberLocal);
                    var regionLocal =
                        region.transform.InverseTransformPoint(world);

                    if (!TryGetCellIndex(
                            regionLocal,
                            bounds,
                            cellSize,
                            gridSize,
                            out var regionIndex))
                    {
                        continue;
                    }

                    occupiedSet.Add(regionIndex);
                }
            }
        }

        private static bool IsInsideAnyMember(
            FloodRegion region,
            Vector3 regionLocalPoint)
        {
            var worldPoint = region.transform.TransformPoint(regionLocalPoint);

            for (var index = 0; index < region.Members.Count; index++)
            {
                var member = region.Members[index];
                if (member.Geometry == null)
                    continue;

                var memberLocal =
                    member.transform.InverseTransformPoint(worldPoint);
                if (member.Geometry.ContainsLocalPoint(memberLocal))
                    return true;
            }

            return false;
        }

        private static bool TryGetCellIndex(
            Vector3 localPoint,
            Bounds bounds,
            Vector3 cellSize,
            Vector3Int gridSize,
            out int flattenedIndex)
        {
            flattenedIndex = -1;
            if (!bounds.Contains(localPoint))
                return false;

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

        private static bool IsFaceConnected(
            HashSet<int> occupied,
            Vector3Int gridSize)
        {
            if (occupied.Count == 0)
                return false;

            var start = -1;
            foreach (var index in occupied)
            {
                start = index;
                break;
            }

            var visited = new HashSet<int>();
            var queue = new Queue<int>();
            queue.Enqueue(start);
            visited.Add(start);

            var xy = gridSize.x * gridSize.y;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var z = current / xy;
                var remainder = current - (z * xy);
                var y = remainder / gridSize.x;
                var x = remainder - (y * gridSize.x);

                TryEnqueueNeighbor(
                    occupied,
                    visited,
                    queue,
                    x - 1,
                    y,
                    z,
                    gridSize);
                TryEnqueueNeighbor(
                    occupied,
                    visited,
                    queue,
                    x + 1,
                    y,
                    z,
                    gridSize);
                TryEnqueueNeighbor(
                    occupied,
                    visited,
                    queue,
                    x,
                    y - 1,
                    z,
                    gridSize);
                TryEnqueueNeighbor(
                    occupied,
                    visited,
                    queue,
                    x,
                    y + 1,
                    z,
                    gridSize);
                TryEnqueueNeighbor(
                    occupied,
                    visited,
                    queue,
                    x,
                    y,
                    z - 1,
                    gridSize);
                TryEnqueueNeighbor(
                    occupied,
                    visited,
                    queue,
                    x,
                    y,
                    z + 1,
                    gridSize);
            }

            return visited.Count == occupied.Count;
        }

        private static void TryEnqueueNeighbor(
            HashSet<int> occupied,
            HashSet<int> visited,
            Queue<int> queue,
            int x,
            int y,
            int z,
            Vector3Int gridSize)
        {
            if (x < 0
                || y < 0
                || z < 0
                || x >= gridSize.x
                || y >= gridSize.y
                || z >= gridSize.z)
            {
                return;
            }

            var index =
                x + (y * gridSize.x) + (z * gridSize.x * gridSize.y);
            if (!occupied.Contains(index) || !visited.Add(index))
                return;

            queue.Enqueue(index);
        }

        private static string CreateFingerprint(FloodRegion region)
        {
            var builder = new StringBuilder();
            builder.Append("FloodRegionData|");
            AppendVector(builder, region.transform.position);
            AppendVector(builder, region.transform.eulerAngles);
            AppendVector(builder, region.transform.lossyScale);
            builder.Append(
                region.CellResolution.ToString(
                    "R",
                    CultureInfo.InvariantCulture)).Append('|');

            for (var index = 0; index < region.Members.Count; index++)
            {
                var member = region.Members[index];
                builder.Append(member.name).Append('#')
                    .Append(index).Append('|');
                builder.Append((int)member.GeometryMode).Append('|');
                AppendVector(builder, member.transform.position);
                AppendVector(builder, member.transform.eulerAngles);
                AppendVector(builder, member.transform.lossyScale);

                switch (member.GeometryMode)
                {
                    case FloodGeometryMode.RectangularPrism:
                        AppendVector(
                            builder,
                            member.Geometry.LocalBounds.size);
                        break;
                    case FloodGeometryMode.ExtrudedPolygon:
                        AppendVector(
                            builder,
                            member.Geometry.LocalBounds.size);
                        if (member.Geometry is IExtrudedFloodVolumeGeometry extruded)
                        {
                            foreach (var point in extruded.Footprint)
                            {
                                builder.Append(
                                        point.x.ToString(
                                            "R",
                                            CultureInfo.InvariantCulture))
                                    .Append(',')
                                    .Append(
                                        point.y.ToString(
                                            "R",
                                            CultureInfo.InvariantCulture))
                                    .Append(';');
                            }
                        }

                        break;
                    case FloodGeometryMode.BakedData:
                        builder.Append(
                            member.BakedVolumeData?.SourceFingerprint
                            ?? string.Empty).Append('|');
                        break;
                }
            }

            return Hash128.Compute(builder.ToString()).ToString();
        }

        private static void AppendVector(
            StringBuilder builder,
            Vector3 value)
        {
            builder.Append(value.x.ToString("R", CultureInfo.InvariantCulture))
                .Append(',')
                .Append(value.y.ToString("R", CultureInfo.InvariantCulture))
                .Append(',')
                .Append(value.z.ToString("R", CultureInfo.InvariantCulture))
                .Append(';');
        }

        private static bool IsFinitePositive(float value)
        {
            return float.IsFinite(value)
                && value > FloodGeometryTolerances.MinimumDimension;
        }
    }
}
