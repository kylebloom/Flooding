using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Builds a closed region-/volume-local presentation-boundary mesh from the
    /// exterior faces of an occupancy union. Shared faces between adjacent
    /// occupied cells are omitted. The mesh is voxel-shaped at cell resolution
    /// and is intended for free-surface plane intersection, not smooth
    /// source-faithful walls.
    /// </summary>
    internal static class OccupancyPresentationBoundaryBuilder
    {
        private static readonly Vector3Int[] NeighborOffsets =
        {
            new Vector3Int(-1, 0, 0),
            new Vector3Int(1, 0, 0),
            new Vector3Int(0, -1, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, 0, -1),
            new Vector3Int(0, 0, 1),
        };

        /// <summary>
        /// Corner indices (in cell-local 0/1 space) for each outward face, ordered
        /// CCW when viewed from outside the cell.
        /// </summary>
        private static readonly Vector3Int[][] FaceCorners =
        {
            // -X
            new[]
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(0, 1, 0),
                new Vector3Int(0, 1, 1),
                new Vector3Int(0, 0, 1),
            },
            // +X
            new[]
            {
                new Vector3Int(1, 0, 0),
                new Vector3Int(1, 0, 1),
                new Vector3Int(1, 1, 1),
                new Vector3Int(1, 1, 0),
            },
            // -Y
            new[]
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(0, 0, 1),
                new Vector3Int(1, 0, 1),
                new Vector3Int(1, 0, 0),
            },
            // +Y
            new[]
            {
                new Vector3Int(0, 1, 0),
                new Vector3Int(1, 1, 0),
                new Vector3Int(1, 1, 1),
                new Vector3Int(0, 1, 1),
            },
            // -Z
            new[]
            {
                new Vector3Int(0, 0, 0),
                new Vector3Int(1, 0, 0),
                new Vector3Int(1, 1, 0),
                new Vector3Int(0, 1, 0),
            },
            // +Z
            new[]
            {
                new Vector3Int(0, 0, 1),
                new Vector3Int(0, 1, 1),
                new Vector3Int(1, 1, 1),
                new Vector3Int(1, 0, 1),
            },
        };

        /// <summary>
        /// Builds welded exterior-face triangles for the occupied cell union.
        /// </summary>
        public static bool TryBuild(
            Bounds localBounds,
            Vector3 cellSize,
            Vector3Int gridSize,
            IReadOnlyList<int> occupiedCellIndices,
            out Vector3[] vertices,
            out int[] triangles,
            out string message)
        {
            vertices = Array.Empty<Vector3>();
            triangles = Array.Empty<int>();

            if (occupiedCellIndices == null || occupiedCellIndices.Count == 0)
            {
                message = "Occupied cells are required.";
                return false;
            }

            if (gridSize.x <= 0 || gridSize.y <= 0 || gridSize.z <= 0)
            {
                message = "Grid size must be positive.";
                return false;
            }

            if (!IsFinitePositive(cellSize.x)
                || !IsFinitePositive(cellSize.y)
                || !IsFinitePositive(cellSize.z))
            {
                message = "Cell size must be finite and positive.";
                return false;
            }

            var occupied = new HashSet<int>(occupiedCellIndices.Count);
            for (var index = 0; index < occupiedCellIndices.Count; index++)
                occupied.Add(occupiedCellIndices[index]);

            var vertexLookup = new Dictionary<CornerKey, int>();
            var vertexList = new List<Vector3>();
            var triangleList = new List<int>();
            var xy = gridSize.x * gridSize.y;

            foreach (var flattened in occupied)
            {
                if (!TryDecode(
                        flattened,
                        gridSize,
                        xy,
                        out var x,
                        out var y,
                        out var z))
                {
                    continue;
                }

                for (var face = 0; face < NeighborOffsets.Length; face++)
                {
                    var offset = NeighborOffsets[face];
                    if (IsOccupiedNeighbor(
                            occupied,
                            x + offset.x,
                            y + offset.y,
                            z + offset.z,
                            gridSize))
                    {
                        continue;
                    }

                    AppendExposedFace(
                        localBounds.min,
                        cellSize,
                        x,
                        y,
                        z,
                        FaceCorners[face],
                        vertexLookup,
                        vertexList,
                        triangleList);
                }
            }

            if (triangleList.Count < 3)
            {
                message =
                    "Occupancy exterior produced no presentation triangles.";
                return false;
            }

            vertices = vertexList.ToArray();
            triangles = triangleList.ToArray();
            message = string.Empty;
            return true;
        }

        private static void AppendExposedFace(
            Vector3 boundsMin,
            Vector3 cellSize,
            int cellX,
            int cellY,
            int cellZ,
            Vector3Int[] corners,
            Dictionary<CornerKey, int> vertexLookup,
            List<Vector3> vertices,
            List<int> triangles)
        {
            var indices = new int[4];
            for (var corner = 0; corner < 4; corner++)
            {
                var local = corners[corner];
                var key = new CornerKey(
                    cellX + local.x,
                    cellY + local.y,
                    cellZ + local.z);
                if (!vertexLookup.TryGetValue(key, out var vertexIndex))
                {
                    vertexIndex = vertices.Count;
                    vertexLookup.Add(key, vertexIndex);
                    vertices.Add(
                        boundsMin + new Vector3(
                            key.X * cellSize.x,
                            key.Y * cellSize.y,
                            key.Z * cellSize.z));
                }

                indices[corner] = vertexIndex;
            }

            // Skip degenerate quads (duplicate welded verts).
            if (indices[0] == indices[1]
                || indices[1] == indices[2]
                || indices[2] == indices[3]
                || indices[3] == indices[0]
                || indices[0] == indices[2]
                || indices[1] == indices[3])
            {
                return;
            }

            triangles.Add(indices[0]);
            triangles.Add(indices[1]);
            triangles.Add(indices[2]);
            triangles.Add(indices[0]);
            triangles.Add(indices[2]);
            triangles.Add(indices[3]);
        }

        private static bool IsOccupiedNeighbor(
            HashSet<int> occupied,
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
                return false;
            }

            var index =
                x + (y * gridSize.x) + (z * gridSize.x * gridSize.y);
            return occupied.Contains(index);
        }

        private static bool TryDecode(
            int flattened,
            Vector3Int gridSize,
            int xy,
            out int x,
            out int y,
            out int z)
        {
            x = 0;
            y = 0;
            z = 0;
            if (flattened < 0 || xy <= 0)
                return false;

            z = flattened / xy;
            var remainder = flattened - (z * xy);
            y = remainder / gridSize.x;
            x = remainder - (y * gridSize.x);
            return x >= 0
                && y >= 0
                && z >= 0
                && x < gridSize.x
                && y < gridSize.y
                && z < gridSize.z;
        }

        private static bool IsFinitePositive(float value)
        {
            return float.IsFinite(value)
                && value > FloodGeometryTolerances.MinimumDimension;
        }

        private readonly struct CornerKey : IEquatable<CornerKey>
        {
            public CornerKey(int x, int y, int z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            public int X { get; }

            public int Y { get; }

            public int Z { get; }

            public bool Equals(CornerKey other)
            {
                return X == other.X && Y == other.Y && Z == other.Z;
            }

            public override bool Equals(object obj)
            {
                return obj is CornerKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = X;
                    hash = (hash * 397) ^ Y;
                    hash = (hash * 397) ^ Z;
                    return hash;
                }
            }
        }
    }
}
