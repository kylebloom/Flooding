using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Immutable, Editor-baked representation of a closed floodable volume.
    /// Runtime code reads this asset but never analyzes its source mesh.
    /// </summary>
    [CreateAssetMenu(
        fileName = "FloodVolumeData",
        menuName = "Flooding/Flood Volume Data")]
    public sealed class FloodVolumeData : ScriptableObject
    {
        internal const int CurrentFormatVersion = 1;

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

        private ReadOnlyCollection<int> readOnlyOccupiedCells;

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

        /// <summary>Gets whether this asset contains supported baked data.</summary>
        public bool IsUsable =>
            formatVersion == CurrentFormatVersion
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

        internal void Initialize(
            Bounds newLocalBounds,
            Vector3 newCellSize,
            Vector3Int newGridSize,
            int[] newOccupiedCellIndices,
            int newBoundaryCellCount,
            string newSourceFingerprint)
        {
            if (newOccupiedCellIndices == null)
                throw new ArgumentNullException(nameof(newOccupiedCellIndices));
            if (newOccupiedCellIndices.Length == 0)
                throw new ArgumentException(
                    "A bake must contain at least one occupied cell.",
                    nameof(newOccupiedCellIndices));

            formatVersion = CurrentFormatVersion;
            localBounds = newLocalBounds;
            cellSize = newCellSize;
            gridSize = newGridSize;
            occupiedCellIndices = (int[])newOccupiedCellIndices.Clone();
            Array.Sort(occupiedCellIndices);
            boundaryCellCount = Math.Max(0, newBoundaryCellCount);
            sourceFingerprint = newSourceFingerprint ?? string.Empty;
            readOnlyOccupiedCells = null;

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
    }
}
