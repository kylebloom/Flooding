using System.Collections.Generic;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Provides deterministic volume queries for one floodable container shape.
    /// Geometry is expressed in the owning FloodVolume's local space.
    /// </summary>
    public interface IFloodVolumeGeometry
    {
        /// <summary>
        /// Gets the total container capacity in cubic meters.
        /// </summary>
        double Capacity { get; }

        /// <summary>
        /// Gets the axis-aligned local-space bounds of the container.
        /// </summary>
        Bounds LocalBounds { get; }

        /// <summary>
        /// Gets whether this implementation can evaluate the supplied local-space plane.
        /// The submerged region is the plane's negative half-space.
        /// </summary>
        bool SupportsPlane(Plane localSurfacePlane);

        /// <summary>
        /// Calculates volume in the plane's negative half-space.
        /// </summary>
        double CalculateSubmergedVolume(Plane localSurfacePlane);

        /// <summary>
        /// Calculates volume, centroid, and free-surface intersection data.
        /// </summary>
        FloodSubmersionResult EvaluateSubmersion(Plane localSurfacePlane);
    }

    /// <summary>
    /// Geometry contract for a constant polygon footprint extruded along local Y.
    /// </summary>
    public interface IExtrudedFloodVolumeGeometry : IFloodVolumeGeometry
    {
        /// <summary>
        /// Gets the normalized counter-clockwise local XZ footprint.
        /// </summary>
        IReadOnlyList<Vector2> Footprint { get; }

        /// <summary>
        /// Gets footprint triangle indices in counter-clockwise XZ order.
        /// </summary>
        IReadOnlyList<int> SurfaceTriangles { get; }

        /// <summary>
        /// Gets the footprint area in square meters.
        /// </summary>
        double FloorArea { get; }

        /// <summary>
        /// Gets the extrusion height in meters.
        /// </summary>
        double MaximumHeight { get; }

        /// <summary>
        /// Gets the local XZ centroid of the footprint.
        /// </summary>
        Vector2 FootprintCentroid { get; }
    }
}
