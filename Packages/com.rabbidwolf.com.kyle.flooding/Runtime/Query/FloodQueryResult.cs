using System;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Read-only gameplay query result for one world-space sample point against
    /// a <see cref="FloodVolume"/>'s current authoritative state.
    /// </summary>
    public readonly struct FloodQueryResult : IEquatable<FloodQueryResult>
    {
        /// <summary>
        /// Creates a query result.
        /// </summary>
        public FloodQueryResult(
            bool isInsideVolume,
            bool isSubmerged,
            float submersionDepthMeters,
            Vector3 surfacePoint,
            Vector3 surfaceNormal)
        {
            IsInsideVolume = isInsideVolume;
            IsSubmerged = isSubmerged;
            SubmersionDepthMeters = submersionDepthMeters;
            SurfacePoint = surfacePoint;
            SurfaceNormal = surfaceNormal;
        }

        /// <summary>
        /// Gets whether the sample point lies inside the floodable compartment
        /// geometry. For baked geometry this uses occupancy-cell approximation;
        /// see <see cref="IFloodVolumeGeometry.ContainmentPrecision"/>.
        /// </summary>
        public bool IsInsideVolume { get; }

        /// <summary>
        /// Gets whether the sample point is inside the compartment and below
        /// the current water surface plane.
        /// </summary>
        public bool IsSubmerged { get; }

        /// <summary>
        /// Gets how far below the water surface the sample point is, in meters.
        /// Zero when the point is not submerged.
        /// </summary>
        public float SubmersionDepthMeters { get; }

        /// <summary>
        /// Gets the closest point on the current water surface plane to the
        /// sample point, in world space.
        /// </summary>
        public Vector3 SurfacePoint { get; }

        /// <summary>
        /// Gets the current water surface plane normal in world space.
        /// </summary>
        public Vector3 SurfaceNormal { get; }

        /// <inheritdoc />
        public bool Equals(FloodQueryResult other)
        {
            return IsInsideVolume == other.IsInsideVolume
                && IsSubmerged == other.IsSubmerged
                && SubmersionDepthMeters.Equals(other.SubmersionDepthMeters)
                && SurfacePoint.Equals(other.SurfacePoint)
                && SurfaceNormal.Equals(other.SurfaceNormal);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is FloodQueryResult other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(
                IsInsideVolume,
                IsSubmerged,
                SubmersionDepthMeters,
                SurfacePoint,
                SurfaceNormal);
        }

        /// <summary>
        /// Determines whether two query results contain the same values.
        /// </summary>
        public static bool operator ==(
            FloodQueryResult left,
            FloodQueryResult right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two query results contain different values.
        /// </summary>
        public static bool operator !=(
            FloodQueryResult left,
            FloodQueryResult right)
        {
            return !left.Equals(right);
        }
    }
}
