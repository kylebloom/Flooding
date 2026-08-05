using System;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Exact two-body union via inclusion-exclusion in region-local space.
    /// </summary>
    /// <remarks>
    /// Prototype representation only. Prefer equal-height extruded presentation
    /// geometry when available for continuous surface meshes.
    /// </remarks>
    internal sealed class TwoBoxInclusionExclusionGeometry :
        IFloodVolumeGeometry,
        IExtrudedFloodVolumeGeometry
    {
        private readonly IFloodVolumeGeometry boxA;
        private readonly IFloodVolumeGeometry boxB;
        private readonly IFloodVolumeGeometry intersection;
        private readonly IExtrudedFloodVolumeGeometry presentation;
        private readonly double capacity;
        private readonly Bounds localBounds;

        public TwoBoxInclusionExclusionGeometry(
            IFloodVolumeGeometry boxA,
            IFloodVolumeGeometry boxB,
            IFloodVolumeGeometry intersection,
            IExtrudedFloodVolumeGeometry presentationGeometry)
        {
            this.boxA = boxA ?? throw new ArgumentNullException(nameof(boxA));
            this.boxB = boxB ?? throw new ArgumentNullException(nameof(boxB));
            this.intersection = intersection;
            presentation = presentationGeometry;

            var intersectionCapacity = intersection?.Capacity ?? 0d;
            capacity = boxA.Capacity + boxB.Capacity - intersectionCapacity;

            var combined = boxA.LocalBounds;
            combined.Encapsulate(boxB.LocalBounds);
            localBounds = combined;
        }

        /// <inheritdoc />
        public double Capacity => capacity;

        /// <inheritdoc />
        public Bounds LocalBounds => localBounds;

        /// <inheritdoc />
        public FloodContainmentPrecision ContainmentPrecision =>
            FloodContainmentPrecision.Exact;

        /// <summary>
        /// Gets extruded presentation geometry when an equal-height footprint
        /// union was constructed; otherwise null.
        /// </summary>
        public IExtrudedFloodVolumeGeometry PresentationGeometry => presentation;

        /// <inheritdoc cref="IExtrudedFloodVolumeGeometry.Footprint" />
        public System.Collections.Generic.IReadOnlyList<Vector2> Footprint =>
            presentation != null
                ? presentation.Footprint
                : Array.Empty<Vector2>();

        /// <inheritdoc cref="IExtrudedFloodVolumeGeometry.SurfaceTriangles" />
        public System.Collections.Generic.IReadOnlyList<int> SurfaceTriangles =>
            presentation != null
                ? presentation.SurfaceTriangles
                : Array.Empty<int>();

        /// <inheritdoc cref="IExtrudedFloodVolumeGeometry.FloorArea" />
        public double FloorArea =>
            presentation != null
                ? presentation.FloorArea
                : capacity / Math.Max(
                    FloodGeometryTolerances.MinimumDimension,
                    localBounds.size.y);

        /// <inheritdoc cref="IExtrudedFloodVolumeGeometry.MaximumHeight" />
        public double MaximumHeight =>
            presentation != null
                ? presentation.MaximumHeight
                : localBounds.size.y;

        /// <inheritdoc cref="IExtrudedFloodVolumeGeometry.FootprintCentroid" />
        public Vector2 FootprintCentroid =>
            presentation != null
                ? presentation.FootprintCentroid
                : new Vector2(localBounds.center.x, localBounds.center.z);

        /// <inheritdoc />
        public bool SupportsPlane(Plane localSurfacePlane)
        {
            return boxA.SupportsPlane(localSurfacePlane)
                && boxB.SupportsPlane(localSurfacePlane);
        }

        /// <inheritdoc />
        public double CalculateSubmergedVolume(Plane localSurfacePlane)
        {
            var volume =
                boxA.CalculateSubmergedVolume(localSurfacePlane)
                + boxB.CalculateSubmergedVolume(localSurfacePlane);

            if (intersection != null)
            {
                volume -= intersection.CalculateSubmergedVolume(
                    localSurfacePlane);
            }

            return Math.Max(0d, Math.Min(capacity, volume));
        }

        /// <inheritdoc />
        public FloodSubmersionResult EvaluateSubmersion(Plane localSurfacePlane)
        {
            if (presentation != null)
                return presentation.EvaluateSubmersion(localSurfacePlane);

            var resultA = boxA.EvaluateSubmersion(localSurfacePlane);
            var resultB = boxB.EvaluateSubmersion(localSurfacePlane);
            var volume = resultA.Volume + resultB.Volume;
            var weighted = (resultA.Centroid * (float)resultA.Volume)
                + (resultB.Centroid * (float)resultB.Volume);

            if (intersection != null)
            {
                var resultI = intersection.EvaluateSubmersion(localSurfacePlane);
                volume -= resultI.Volume;
                weighted -= resultI.Centroid * (float)resultI.Volume;
            }

            volume = Math.Max(0d, Math.Min(capacity, volume));
            var centroid = volume > FloodGeometryTolerances.SolverAbsoluteVolume
                ? weighted / (float)volume
                : localBounds.center;

            return new FloodSubmersionResult(
                volume,
                centroid,
                default);
        }

        /// <inheritdoc />
        public bool ContainsLocalPoint(Vector3 localPoint)
        {
            return boxA.ContainsLocalPoint(localPoint)
                || boxB.ContainsLocalPoint(localPoint);
        }
    }
}
