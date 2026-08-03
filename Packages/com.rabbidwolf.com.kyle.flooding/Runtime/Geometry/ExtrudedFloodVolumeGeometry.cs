using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Shared arbitrary-plane behavior for polygon prisms extruded along local Y.
    /// </summary>
    public abstract class ExtrudedFloodVolumeGeometry :
        IExtrudedFloodVolumeGeometry
    {
        private readonly ReadOnlyCollection<Vector2> footprint;
        private readonly ReadOnlyCollection<int> surfaceTriangles;

        protected ExtrudedFloodVolumeGeometry(
            Vector2[] footprint,
            int[] surfaceTriangles,
            double floorArea,
            double maximumHeight,
            Vector2 footprintCentroid,
            Bounds localBounds)
        {
            if (footprint == null)
                throw new ArgumentNullException(nameof(footprint));
            if (surfaceTriangles == null)
                throw new ArgumentNullException(nameof(surfaceTriangles));

            this.footprint = Array.AsReadOnly((Vector2[])footprint.Clone());
            this.surfaceTriangles = Array.AsReadOnly(
                (int[])surfaceTriangles.Clone());

            FloorArea = floorArea;
            MaximumHeight = maximumHeight;
            FootprintCentroid = footprintCentroid;
            LocalBounds = localBounds;
            Capacity = floorArea * maximumHeight;
        }

        /// <inheritdoc />
        public IReadOnlyList<Vector2> Footprint => footprint;

        /// <inheritdoc />
        public IReadOnlyList<int> SurfaceTriangles => surfaceTriangles;

        /// <inheritdoc />
        public double FloorArea { get; }

        /// <inheritdoc />
        public double MaximumHeight { get; }

        /// <inheritdoc />
        public Vector2 FootprintCentroid { get; }

        /// <inheritdoc />
        public double Capacity { get; }

        /// <inheritdoc />
        public Bounds LocalBounds { get; }

        /// <inheritdoc />
        public bool SupportsPlane(Plane localSurfacePlane)
        {
            var normal = localSurfacePlane.normal;

            return !float.IsNaN(normal.x)
                && !float.IsNaN(normal.y)
                && !float.IsNaN(normal.z)
                && !float.IsInfinity(normal.x)
                && !float.IsInfinity(normal.y)
                && !float.IsInfinity(normal.z)
                && normal.sqrMagnitude
                    > FloodGeometryTolerances.PlaneNormal
                        * FloodGeometryTolerances.PlaneNormal
                && !float.IsNaN(localSurfacePlane.distance)
                && !float.IsInfinity(localSurfacePlane.distance);
        }

        /// <inheritdoc />
        public double CalculateSubmergedVolume(Plane localSurfacePlane)
        {
            return FloodExtrudedGeometryQueries.Evaluate(
                this,
                localSurfacePlane,
                includeSurfaceIntersection: false).Volume;
        }

        /// <inheritdoc />
        public FloodSubmersionResult EvaluateSubmersion(
            Plane localSurfacePlane)
        {
            return FloodExtrudedGeometryQueries.Evaluate(
                this,
                localSurfacePlane,
                includeSurfaceIntersection: true);
        }
    }
}
