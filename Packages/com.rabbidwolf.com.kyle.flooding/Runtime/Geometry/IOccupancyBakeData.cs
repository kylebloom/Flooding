using System.Collections.Generic;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Shared contract for Editor-baked occupancy assets consumed by
    /// <see cref="BakedFloodGeometry"/>.
    /// </summary>
    internal interface IOccupancyBakeData
    {
        bool IsUsable { get; }

        double Capacity { get; }

        Bounds LocalBounds { get; }

        Vector3 CellSize { get; }

        Vector3Int GridSize { get; }

        Vector3 Centroid { get; }

        IReadOnlyList<int> OccupiedCellIndices { get; }

        bool HasPresentationBoundary { get; }

        IReadOnlyList<Vector3> PresentationBoundaryVertices { get; }

        IReadOnlyList<int> PresentationBoundaryTriangles { get; }

        Vector3 GetCellCenter(int flattenedIndex);
    }
}
