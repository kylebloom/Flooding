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
    /// Editor-only validation and center-sampled cell baking for closed meshes.
    /// </summary>
    internal static class FloodVolumeBaker
    {
        public static bool TryBake(
            FloodVolumeAuthoring authoring,
            out FloodVolumeData data,
            out string message)
        {
            data = null;
            if (!TryGetSource(
                    authoring,
                    out var target,
                    out var mesh,
                    out var sourceToTarget,
                    out message))
            {
                return false;
            }

            if (!TryValidateClosedMesh(mesh, out message))
                return false;

            var sourceVertices = mesh.vertices;
            var vertices = new Vector3[sourceVertices.Length];
            for (var index = 0; index < vertices.Length; index++)
                vertices[index] = sourceToTarget.MultiplyPoint3x4(
                    sourceVertices[index]);

            var triangles = mesh.triangles;
            var bounds = CreateBounds(vertices);

            if (!IsFinitePositive(bounds.size.x)
                || !IsFinitePositive(bounds.size.y)
                || !IsFinitePositive(bounds.size.z))
            {
                message =
                    "The source-to-volume Transform produces zero or non-finite "
                    + "bounds. Check source and target Transform scale.";
                return false;
            }

            var gridSize = new Vector3Int(
                Mathf.Max(
                    1,
                    Mathf.CeilToInt(
                        bounds.size.x / authoring.CellResolution)),
                Mathf.Max(
                    1,
                    Mathf.CeilToInt(
                        bounds.size.y / authoring.CellResolution)),
                Mathf.Max(
                    1,
                    Mathf.CeilToInt(
                        bounds.size.z / authoring.CellResolution)));
            var totalGridCells =
                (long)gridSize.x * gridSize.y * gridSize.z;

            if (totalGridCells > authoring.MaximumGridCells)
            {
                message =
                    $"Bake requires {totalGridCells:N0} grid cells, exceeding "
                    + $"the configured limit of {authoring.MaximumGridCells:N0}. "
                    + "Increase Cell Resolution or the safety limit.";
                return false;
            }

            var cellSize = new Vector3(
                bounds.size.x / gridSize.x,
                bounds.size.y / gridSize.y,
                bounds.size.z / gridSize.z);
            var occupied = new List<int>();
            var boundaryCellCount = 0;
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
                        var centerInside = IsPointInside(
                            center,
                            vertices,
                            triangles);
                        var hasInsideCorner = false;
                        var hasOutsideCorner = false;

                        for (var corner = 0; corner < 8; corner++)
                        {
                            var point = minimum + new Vector3(
                                (corner & 1) == 0 ? 0f : cellSize.x,
                                (corner & 2) == 0 ? 0f : cellSize.y,
                                (corner & 4) == 0 ? 0f : cellSize.z);
                            if (IsPointInside(point, vertices, triangles))
                                hasInsideCorner = true;
                            else
                                hasOutsideCorner = true;
                        }

                        if (hasInsideCorner && hasOutsideCorner)
                            boundaryCellCount++;
                        if (centerInside)
                            occupied.Add(flattenedIndex);

                        flattenedIndex++;
                    }
                }
            }

            if (occupied.Count == 0)
            {
                message =
                    "The selected resolution produced no occupied cells. "
                    + "Use a smaller Cell Resolution or a larger closed mesh.";
                return false;
            }

            var fingerprint = CreateFingerprint(
                mesh,
                sourceToTarget,
                authoring.CellResolution);
            data = authoring.BakedData;

            if (data == null)
            {
                var path = EditorUtility.SaveFilePanelInProject(
                    "Save Flood Volume Data",
                    $"{target.name} FloodVolumeData",
                    "asset",
                    "Choose a project asset path for the immutable bake.");
                if (string.IsNullOrEmpty(path))
                {
                    message = "Bake cancelled before creating an asset.";
                    return false;
                }

                data = ScriptableObject.CreateInstance<FloodVolumeData>();
                AssetDatabase.CreateAsset(data, path);
            }

            Undo.RecordObject(data, "Bake Flood Volume Data");
            data.Initialize(
                bounds,
                cellSize,
                gridSize,
                occupied.ToArray(),
                boundaryCellCount,
                fingerprint);
            EditorUtility.SetDirty(data);

            Undo.RecordObject(authoring, "Assign Flood Volume Bake");
            Undo.RecordObject(target, "Assign Flood Volume Bake");
            authoring.AssignBake(data);
            EditorUtility.SetDirty(authoring);
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();

            message =
                $"Baked {occupied.Count:N0} occupied cells "
                + $"({data.Capacity:0.###} m³) from "
                + $"{totalGridCells:N0} inspected cells.";
            return true;
        }

        public static bool TryGetStatus(
            FloodVolumeAuthoring authoring,
            out bool stale,
            out string message)
        {
            stale = false;
            if (!TryGetSource(
                    authoring,
                    out var target,
                    out var mesh,
                    out var sourceToTarget,
                    out message))
            {
                return false;
            }

            if (authoring.BakedData == null)
            {
                message = "No Flood Volume Data asset is assigned. Bake required.";
                return false;
            }

            if (!authoring.BakedData.IsUsable)
            {
                message =
                    "Assigned bake is empty or uses an unsupported format. Re-bake required.";
                return false;
            }

            var currentFingerprint = CreateFingerprint(
                mesh,
                sourceToTarget,
                authoring.CellResolution);
            stale = !string.Equals(
                currentFingerprint,
                authoring.BakedData.SourceFingerprint,
                StringComparison.Ordinal);

            if (stale)
            {
                message =
                    "Bake is stale: the source mesh, source-to-volume transform, "
                    + "or Cell Resolution changed.";
                return true;
            }

            if (target.GeometryMode != FloodGeometryMode.BakedData
                || target.BakedVolumeData != authoring.BakedData)
            {
                stale = true;
                message =
                    "Bake asset is current but the target Flood Volume is not "
                    + "configured to use it. Bake again to repair the assignment.";
                return true;
            }

            message = "Bake is current.";
            return true;
        }

        private static bool TryGetSource(
            FloodVolumeAuthoring authoring,
            out FloodVolume target,
            out Mesh mesh,
            out Matrix4x4 sourceToTarget,
            out string message)
        {
            target = authoring.TargetVolume;
            mesh = authoring.SourceMeshFilter == null
                ? null
                : authoring.SourceMeshFilter.sharedMesh;
            sourceToTarget = default;

            if (target == null)
            {
                message = "Assign a target Flood Volume component.";
                return false;
            }
            if (authoring.SourceMeshFilter == null || mesh == null)
            {
                message =
                    "Assign a Mesh Filter with a closed readable source mesh.";
                return false;
            }
            if (!mesh.isReadable)
            {
                message =
                    $"Mesh '{mesh.name}' is not readable. Enable Read/Write in "
                    + "its model import settings for Editor baking.";
                return false;
            }

            sourceToTarget =
                target.transform.worldToLocalMatrix
                * authoring.SourceMeshFilter.transform.localToWorldMatrix;
            message = string.Empty;
            return true;
        }

        internal static bool TryValidateClosedMesh(
            Mesh mesh,
            out string message)
        {
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;

            if (vertices.Length < 4 || triangles.Length < 12)
            {
                message =
                    "Source mesh must contain a closed three-dimensional volume.";
                return false;
            }
            if (triangles.Length % 3 != 0)
            {
                message = "Source mesh triangle index count is invalid.";
                return false;
            }

            var edgeUse = new Dictionary<ulong, int>();
            var canonicalByPosition = new Dictionary<Vector3, int>();
            var canonicalIndices = new int[vertices.Length];
            foreach (var vertex in vertices)
            {
                if (float.IsFinite(vertex.x)
                    && float.IsFinite(vertex.y)
                    && float.IsFinite(vertex.z))
                {
                    continue;
                }

                message = "Source mesh contains a non-finite vertex.";
                return false;
            }

            for (var index = 0; index < vertices.Length; index++)
            {
                if (!canonicalByPosition.TryGetValue(
                        vertices[index],
                        out var canonicalIndex))
                {
                    canonicalIndex = canonicalByPosition.Count;
                    canonicalByPosition.Add(
                        vertices[index],
                        canonicalIndex);
                }

                canonicalIndices[index] = canonicalIndex;
            }

            for (var index = 0; index < triangles.Length; index += 3)
            {
                var first = triangles[index];
                var second = triangles[index + 1];
                var third = triangles[index + 2];

                if (first < 0
                    || first >= vertices.Length
                    || second < 0
                    || second >= vertices.Length
                    || third < 0
                    || third >= vertices.Length)
                {
                    message =
                        $"Source mesh triangle {index / 3} contains an invalid vertex index.";
                    return false;
                }

                var areaVector = Vector3.Cross(
                    vertices[second] - vertices[first],
                    vertices[third] - vertices[first]);

                if (areaVector.sqrMagnitude
                    <= FloodGeometryTolerances.Position
                        * FloodGeometryTolerances.Position)
                {
                    message =
                        $"Source mesh contains a degenerate triangle at index {index / 3}.";
                    return false;
                }

                CountEdge(
                    edgeUse,
                    canonicalIndices[first],
                    canonicalIndices[second]);
                CountEdge(
                    edgeUse,
                    canonicalIndices[second],
                    canonicalIndices[third]);
                CountEdge(
                    edgeUse,
                    canonicalIndices[third],
                    canonicalIndices[first]);
            }

            foreach (var pair in edgeUse)
            {
                if (pair.Value == 2)
                    continue;

                message =
                    "Source mesh is open or non-manifold. Every undirected "
                    + $"triangle edge must be used exactly twice; one edge is used {pair.Value} time(s).";
                return false;
            }

            message = string.Empty;
            return true;
        }

        private static void CountEdge(
            Dictionary<ulong, int> edgeUse,
            int first,
            int second)
        {
            var minimum = (uint)Math.Min(first, second);
            var maximum = (uint)Math.Max(first, second);
            var key = ((ulong)minimum << 32) | maximum;
            edgeUse.TryGetValue(key, out var count);
            edgeUse[key] = count + 1;
        }

        private static Bounds CreateBounds(IReadOnlyList<Vector3> vertices)
        {
            var bounds = new Bounds(vertices[0], Vector3.zero);
            for (var index = 1; index < vertices.Count; index++)
                bounds.Encapsulate(vertices[index]);
            return bounds;
        }

        private static bool IsPointInside(
            Vector3 point,
            IReadOnlyList<Vector3> vertices,
            IReadOnlyList<int> triangles)
        {
            var direction = new Vector3(1f, 0.37139067f, 0.529847f).normalized;
            var intersections = new List<float>();

            for (var index = 0; index < triangles.Count; index += 3)
            {
                if (TryRayTriangle(
                        point,
                        direction,
                        vertices[triangles[index]],
                        vertices[triangles[index + 1]],
                        vertices[triangles[index + 2]],
                        out var distance))
                {
                    var duplicate = false;
                    foreach (var existing in intersections)
                    {
                        if (Mathf.Abs(existing - distance)
                            <= FloodGeometryTolerances.Position)
                        {
                            duplicate = true;
                            break;
                        }
                    }

                    if (!duplicate)
                        intersections.Add(distance);
                }
            }

            return intersections.Count % 2 == 1;
        }

        private static bool TryRayTriangle(
            Vector3 origin,
            Vector3 direction,
            Vector3 first,
            Vector3 second,
            Vector3 third,
            out float distance)
        {
            var firstEdge = second - first;
            var secondEdge = third - first;
            var perpendicular = Vector3.Cross(direction, secondEdge);
            var determinant = Vector3.Dot(firstEdge, perpendicular);

            if (Mathf.Abs(determinant)
                <= FloodGeometryTolerances.Position)
            {
                distance = 0f;
                return false;
            }

            var inverse = 1f / determinant;
            var offset = origin - first;
            var u = Vector3.Dot(offset, perpendicular) * inverse;
            if (u < 0f || u > 1f)
            {
                distance = 0f;
                return false;
            }

            var cross = Vector3.Cross(offset, firstEdge);
            var v = Vector3.Dot(direction, cross) * inverse;
            if (v < 0f || u + v > 1f)
            {
                distance = 0f;
                return false;
            }

            distance = Vector3.Dot(secondEdge, cross) * inverse;
            return distance > FloodGeometryTolerances.Position;
        }

        private static string CreateFingerprint(
            Mesh mesh,
            Matrix4x4 sourceToTarget,
            float resolution)
        {
            var builder = new StringBuilder();
            var assetPath = AssetDatabase.GetAssetPath(mesh);

            if (!string.IsNullOrEmpty(assetPath))
            {
                builder.Append(
                    AssetDatabase.GetAssetDependencyHash(assetPath));
            }
            else
            {
                foreach (var vertex in mesh.vertices)
                    AppendVector(builder, vertex);
                foreach (var index in mesh.triangles)
                    builder.Append(index).Append(';');
            }

            for (var row = 0; row < 4; row++)
            {
                for (var column = 0; column < 4; column++)
                {
                    builder.Append(
                        sourceToTarget[row, column].ToString(
                            "R",
                            CultureInfo.InvariantCulture)).Append(';');
                }
            }

            builder.Append(
                resolution.ToString("R", CultureInfo.InvariantCulture));
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
