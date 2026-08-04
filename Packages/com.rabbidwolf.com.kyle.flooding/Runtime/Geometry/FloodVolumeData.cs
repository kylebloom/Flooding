using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Immutable, Editor-baked representation of a closed floodable volume.
    /// Runtime code reads this asset but never analyzes its source mesh.
    /// Occupancy cells answer quantity; optional presentation-boundary triangles
    /// answer free-surface footprint shape.
    /// </summary>
    [CreateAssetMenu(
        fileName = "FloodVolumeData",
        menuName = "Flooding/Flood Volume Data")]
    public sealed class FloodVolumeData : ScriptableObject
    {
        /// <summary>
        /// Format for bakes that include optional presentation-boundary mesh data.
        /// </summary>
        internal const int CurrentFormatVersion = 2;

        /// <summary>Legacy occupancy-only bake format.</summary>
        internal const int LegacyFormatVersion = 1;

        private const int PresentationBoundaryTriangleWarning = 50000;

        [SerializeField]
        private int formatVersion;

        [SerializeField]
        private Bounds localBounds;

        [SerializeField]
        private Vector3 cellSize;

        [SerializeField]
        private Vector3Int gridSize;

        [SerializeField]
        private int[] occupiedCellIndices = Array.Empty<int>();

        [SerializeField]
        private double capacity;

        [SerializeField]
        private Vector3 centroid;

        [SerializeField]
        private int boundaryCellCount;

        [SerializeField]
        private double estimatedBoundaryVolume;

        [SerializeField]
        private string sourceFingerprint = string.Empty;

        [SerializeField]
        private Vector3[] presentationBoundaryVertices = Array.Empty<Vector3>();

        [SerializeField]
        private int[] presentationBoundaryTriangles = Array.Empty<int>();

        private ReadOnlyCollection<int> readOnlyOccupiedCells;
        private ReadOnlyCollection<Vector3> readOnlyPresentationVertices;
        private ReadOnlyCollection<int> readOnlyPresentationTriangles;

        /// <summary>Gets the serialized bake format version.</summary>
        public int FormatVersion => formatVersion;

        /// <summary>Gets the baked representation's local-space bounds.</summary>
        public Bounds LocalBounds => localBounds;

        /// <summary>Gets actual local X/Y/Z sample resolution in meters.</summary>
        public Vector3 SampleResolution => cellSize;

        /// <summary>
        /// Gets the number of retained samples in the baked representation.
        /// </summary>
        public int SampleCount => occupiedCellIndices?.Length ?? 0;

        /// <summary>Gets baked capacity in cubic meters.</summary>
        public double Capacity => capacity;

        /// <summary>Gets the full baked volume's local-space centroid.</summary>
        public Vector3 Centroid => centroid;

        /// <summary>
        /// Gets a resolution-dependent approximation indicator in cubic meters.
        /// This is a design diagnostic, not a certified source-mesh error bound.
        /// </summary>
        public double EstimatedApproximationVolume => estimatedBoundaryVolume;

        /// <summary>
        /// Gets whether a usable presentation-boundary mesh is present for
        /// free-surface footprint generation.
        /// </summary>
        public bool HasPresentationBoundary =>
            ValidatePresentationBoundary(
                presentationBoundaryVertices,
                presentationBoundaryTriangles);

        /// <summary>Gets whether this asset contains supported baked data.</summary>
        public bool IsUsable =>
            (formatVersion == LegacyFormatVersion
                || formatVersion == CurrentFormatVersion)
            && gridSize.x > 0
            && gridSize.y > 0
            && gridSize.z > 0
            && cellSize.x > 0f
            && cellSize.y > 0f
            && cellSize.z > 0f
            && SampleCount > 0
            && double.IsFinite(capacity)
            && capacity > 0d
            && HasValidOccupiedCells();

        internal Vector3 CellSize => cellSize;

        internal Vector3Int GridSize => gridSize;

        internal IReadOnlyList<int> OccupiedCellIndices =>
            readOnlyOccupiedCells ??=
                Array.AsReadOnly(occupiedCellIndices ?? Array.Empty<int>());

        internal int BoundaryCellCount => boundaryCellCount;

        internal string SourceFingerprint => sourceFingerprint;

        internal IReadOnlyList<Vector3> PresentationBoundaryVertices =>
            readOnlyPresentationVertices ??=
                Array.AsReadOnly(
                    presentationBoundaryVertices ?? Array.Empty<Vector3>());

        internal IReadOnlyList<int> PresentationBoundaryTriangles =>
            readOnlyPresentationTriangles ??=
                Array.AsReadOnly(
                    presentationBoundaryTriangles ?? Array.Empty<int>());

        internal int PresentationBoundaryVertexCount =>
            presentationBoundaryVertices?.Length ?? 0;

        internal int PresentationBoundaryTriangleCount =>
            (presentationBoundaryTriangles?.Length ?? 0) / 3;

        internal static int PresentationBoundaryTriangleWarningThreshold =>
            PresentationBoundaryTriangleWarning;

        internal void Initialize(
            Bounds newLocalBounds,
            Vector3 newCellSize,
            Vector3Int newGridSize,
            int[] newOccupiedCellIndices,
            int newBoundaryCellCount,
            string newSourceFingerprint,
            Vector3[] newPresentationBoundaryVertices = null,
            int[] newPresentationBoundaryTriangles = null)
        {
            if (newOccupiedCellIndices == null)
                throw new ArgumentNullException(nameof(newOccupiedCellIndices));
            if (newOccupiedCellIndices.Length == 0)
                throw new ArgumentException(
                    "A bake must contain at least one occupied cell.",
                    nameof(newOccupiedCellIndices));

            localBounds = newLocalBounds;
            cellSize = newCellSize;
            gridSize = newGridSize;
            occupiedCellIndices = (int[])newOccupiedCellIndices.Clone();
            Array.Sort(occupiedCellIndices);
            boundaryCellCount = Math.Max(0, newBoundaryCellCount);
            sourceFingerprint = newSourceFingerprint ?? string.Empty;
            readOnlyOccupiedCells = null;
            readOnlyPresentationVertices = null;
            readOnlyPresentationTriangles = null;

            var hasBoundary = ValidatePresentationBoundary(
                newPresentationBoundaryVertices,
                newPresentationBoundaryTriangles);

            if (hasBoundary)
            {
                formatVersion = CurrentFormatVersion;
                presentationBoundaryVertices =
                    (Vector3[])newPresentationBoundaryVertices.Clone();
                presentationBoundaryTriangles =
                    (int[])newPresentationBoundaryTriangles.Clone();
            }
            else
            {
                formatVersion = LegacyFormatVersion;
                presentationBoundaryVertices = Array.Empty<Vector3>();
                presentationBoundaryTriangles = Array.Empty<int>();
            }

            var cellVolume =
                (double)cellSize.x * cellSize.y * cellSize.z;
            capacity = cellVolume * occupiedCellIndices.Length;
            estimatedBoundaryVolume = cellVolume * boundaryCellCount;

            var weightedCenter = Vector3.zero;
            foreach (var flattenedIndex in occupiedCellIndices)
                weightedCenter += GetCellCenter(flattenedIndex);

            centroid = weightedCenter / occupiedCellIndices.Length;
        }

        internal Vector3 GetCellCenter(int flattenedIndex)
        {
            var xy = gridSize.x * gridSize.y;
            var z = flattenedIndex / xy;
            var remainder = flattenedIndex - (z * xy);
            var y = remainder / gridSize.x;
            var x = remainder - (y * gridSize.x);
            var minimum = localBounds.min;

            return minimum + new Vector3(
                (x + 0.5f) * cellSize.x,
                (y + 0.5f) * cellSize.y,
                (z + 0.5f) * cellSize.z);
        }

        private bool HasValidOccupiedCells()
        {
            var maximumIndex =
                (long)gridSize.x * gridSize.y * gridSize.z;
            var previous = -1;

            foreach (var index in occupiedCellIndices)
            {
                if (index < 0
                    || index >= maximumIndex
                    || index <= previous)
                {
                    return false;
                }

                previous = index;
            }

            return true;
        }

        private static bool ValidatePresentationBoundary(
            Vector3[] vertices,
            int[] triangles)
        {
            if (vertices == null
                || triangles == null
                || vertices.Length < 3
                || triangles.Length < 3
                || triangles.Length % 3 != 0)
            {
                return false;
            }

            for (var index = 0; index < vertices.Length; index++)
            {
                var vertex = vertices[index];
                if (!float.IsFinite(vertex.x)
                    || !float.IsFinite(vertex.y)
                    || !float.IsFinite(vertex.z))
                {
                    return false;
                }
            }

            var hasNonDegenerate = false;
            var areaTolerance =
                FloodGeometryTolerances.Position
                * FloodGeometryTolerances.Position;

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
                    return false;
                }

                var areaVector = Vector3.Cross(
                    vertices[second] - vertices[first],
                    vertices[third] - vertices[first]);
                if (areaVector.sqrMagnitude > areaTolerance)
                    hasNonDegenerate = true;
            }

            return hasNonDegenerate;
        }
    }
}
