using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// One ordered closed contour where a water plane intersects container geometry.
    /// The final vertex is implicitly connected to the first.
    /// </summary>
    public readonly struct FloodSurfaceContour
    {
        private readonly ReadOnlyCollection<Vector3> vertices;

        internal FloodSurfaceContour(Vector3[] vertices)
        {
            if (vertices == null)
                throw new ArgumentNullException(nameof(vertices));

            this.vertices = Array.AsReadOnly((Vector3[])vertices.Clone());
        }

        /// <summary>
        /// Gets ordered local-space contour vertices.
        /// </summary>
        public IReadOnlyList<Vector3> Vertices
        {
            get
            {
                if (vertices != null)
                    return vertices;

                return Array.Empty<Vector3>();
            }
        }
    }

    /// <summary>
    /// Immutable free-surface intersection data for one geometry query.
    /// Multiple contours are supported for future baked or disconnected geometry.
    /// </summary>
    public readonly struct FloodSurfaceIntersection
    {
        private readonly ReadOnlyCollection<FloodSurfaceContour> contours;

        internal FloodSurfaceIntersection(FloodSurfaceContour[] contours)
        {
            if (contours == null)
                throw new ArgumentNullException(nameof(contours));

            this.contours = Array.AsReadOnly(
                (FloodSurfaceContour[])contours.Clone());
        }

        /// <summary>
        /// Gets ordered closed contours in container-local space.
        /// </summary>
        public IReadOnlyList<FloodSurfaceContour> Contours
        {
            get
            {
                if (contours != null)
                    return contours;

                return Array.Empty<FloodSurfaceContour>();
            }
        }

        /// <summary>
        /// Gets whether the plane intersects the container interior.
        /// </summary>
        public bool HasSurface => contours != null && contours.Count > 0;
    }

    /// <summary>
    /// Immutable result of evaluating container geometry beneath a surface plane.
    /// </summary>
    public readonly struct FloodSubmersionResult
    {
        internal FloodSubmersionResult(
            double volume,
            Vector3 centroid,
            FloodSurfaceIntersection surfaceIntersection)
        {
            Volume = volume;
            Centroid = centroid;
            SurfaceIntersection = surfaceIntersection;
        }

        /// <summary>
        /// Gets submerged volume in cubic meters.
        /// </summary>
        public double Volume { get; }

        /// <summary>
        /// Gets the submerged region's local-space centroid.
        /// Empty regions use the nearest boundary centroid.
        /// </summary>
        public Vector3 Centroid { get; }

        /// <summary>
        /// Gets free-surface intersection contours.
        /// </summary>
        public FloodSurfaceIntersection SurfaceIntersection { get; }
    }
}
