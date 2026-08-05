using System;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Axis-aligned box in region-local space, expressed as an extruded
    /// footprint with a Y origin offset so floor need not sit at Y=0.
    /// </summary>
    internal sealed class AxisAlignedBoxFloodGeometry : IFloodVolumeGeometry
    {
        private readonly ExtrudedPolygonFloodGeometry extruded;
        private readonly Vector3 originOffset;
        private readonly Bounds localBounds;

        private AxisAlignedBoxFloodGeometry(
            ExtrudedPolygonFloodGeometry extruded,
            Vector3 originOffset,
            Bounds localBounds)
        {
            this.extruded = extruded;
            this.originOffset = originOffset;
            this.localBounds = localBounds;
        }

        public static AxisAlignedBoxFloodGeometry Create(Bounds bounds)
        {
            var size = bounds.size;
            if (size.x < FloodGeometryTolerances.MinimumDimension
                || size.y < FloodGeometryTolerances.MinimumDimension
                || size.z < FloodGeometryTolerances.MinimumDimension)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(bounds),
                    "Box dimensions must be finite and above the minimum.");
            }

            var footprint = new[]
            {
                new Vector2(bounds.min.x, bounds.min.z),
                new Vector2(bounds.max.x, bounds.min.z),
                new Vector2(bounds.max.x, bounds.max.z),
                new Vector2(bounds.min.x, bounds.max.z),
            };

            // Extruded geometry uses floor at Y=0; shift queries by -min.y.
            var extruded = new ExtrudedPolygonFloodGeometry(footprint, size.y);
            var offset = new Vector3(0f, bounds.min.y, 0f);
            return new AxisAlignedBoxFloodGeometry(extruded, offset, bounds);
        }

        /// <inheritdoc />
        public double Capacity => extruded.Capacity;

        /// <inheritdoc />
        public Bounds LocalBounds => localBounds;

        /// <inheritdoc />
        public FloodContainmentPrecision ContainmentPrecision =>
            FloodContainmentPrecision.Exact;

        /// <inheritdoc />
        public bool SupportsPlane(Plane localSurfacePlane)
        {
            return extruded.SupportsPlane(ToExtrudedPlane(localSurfacePlane));
        }

        /// <inheritdoc />
        public double CalculateSubmergedVolume(Plane localSurfacePlane)
        {
            return extruded.CalculateSubmergedVolume(
                ToExtrudedPlane(localSurfacePlane));
        }

        /// <inheritdoc />
        public FloodSubmersionResult EvaluateSubmersion(Plane localSurfacePlane)
        {
            var result = extruded.EvaluateSubmersion(
                ToExtrudedPlane(localSurfacePlane));
            var centroid = result.Centroid + originOffset;

            FloodSurfaceContour[] contours = null;
            if (result.SurfaceIntersection.HasSurface)
            {
                var source = result.SurfaceIntersection.Contours;
                contours = new FloodSurfaceContour[source.Count];
                for (var index = 0; index < source.Count; index++)
                {
                    var vertices = source[index].Vertices;
                    var shifted = new Vector3[vertices.Count];
                    for (var vertexIndex = 0;
                         vertexIndex < vertices.Count;
                         vertexIndex++)
                    {
                        shifted[vertexIndex] =
                            vertices[vertexIndex] + originOffset;
                    }

                    contours[index] = new FloodSurfaceContour(shifted);
                }
            }

            return new FloodSubmersionResult(
                result.Volume,
                centroid,
                contours == null
                    ? default
                    : new FloodSurfaceIntersection(contours));
        }

        /// <inheritdoc />
        public bool ContainsLocalPoint(Vector3 localPoint)
        {
            return extruded.ContainsLocalPoint(localPoint - originOffset);
        }

        internal IExtrudedFloodVolumeGeometry ExtrudedGeometry => extruded;

        internal Vector3 OriginOffset => originOffset;

        private Plane ToExtrudedPlane(Plane regionLocalPlane)
        {
            // geometryLocal = regionLocal - originOffset
            var matrix = Matrix4x4.Translate(-originOffset);
            var normal = regionLocalPlane.normal.normalized;
            var point = -normal * regionLocalPlane.distance;
            var destinationPoint = matrix.MultiplyPoint3x4(point);
            var destinationNormal = matrix.inverse.transpose
                .MultiplyVector(normal)
                .normalized;
            return new Plane(destinationNormal, destinationPoint);
        }
    }
}
