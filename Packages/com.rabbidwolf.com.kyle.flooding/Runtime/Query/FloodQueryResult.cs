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
        /// <param name="isInsideVolume">
        /// Whether the sample lies inside floodable compartment geometry.
        /// </param>
        /// <param name="isSubmerged">
        /// Whether the sample is inside the compartment and below the surface.
        /// </param>
        /// <param name="submersionDepthMeters">
        /// Depth below the surface in meters when submerged; otherwise zero.
        /// Does not change meaning when
        /// <paramref name="surfaceSignedDistanceMeters"/> is supplied.
        /// </param>
        /// <param name="surfacePoint">
        /// Closest point on the current water surface plane, world space.
        /// </param>
        /// <param name="surfaceNormal">
        /// Current water surface plane normal, world space.
        /// </param>
        /// <param name="surfaceSignedDistanceMeters">
        /// Signed distance to the authoritative world-space surface plane in
        /// meters. Positive means the sample is above the plane (along the
        /// surface normal), zero means on the plane, and negative means below
        /// the plane. Independent of compartment containment.
        /// </param>
        public FloodQueryResult(
            bool isInsideVolume,
            bool isSubmerged,
            float submersionDepthMeters,
            Vector3 surfacePoint,
            Vector3 surfaceNormal,
            float surfaceSignedDistanceMeters)
        {
            IsInsideVolume = isInsideVolume;
            IsSubmerged = isSubmerged;
            SubmersionDepthMeters = submersionDepthMeters;
            SurfacePoint = surfacePoint;
            SurfaceNormal = surfaceNormal;
            SurfaceSignedDistanceMeters = surfaceSignedDistanceMeters;
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

        /// <summary>
        /// Gets the signed distance from the sample point to the authoritative
        /// world-space flood surface plane, in meters.
        /// </summary>
        /// <remarks>
        /// Sign convention (surface normal points out of the water):
        /// <list type="bullet">
        /// <item><description>
        /// <c>&gt; 0</c> — sample is above the flood surface
        /// </description></item>
        /// <item><description>
        /// <c>== 0</c> — sample lies on the flood surface
        /// </description></item>
        /// <item><description>
        /// <c>&lt; 0</c> — sample is below the flood surface
        /// </description></item>
        /// </list>
        /// This value is derived from the same world-space
        /// <see cref="FloodVolume.SurfacePlane"/> used for submersion depth.
        /// It is reported even when the sample is outside the compartment
        /// geometry. <see cref="SubmersionDepthMeters"/> remains non-negative
        /// and zero when the point is not submerged.
        /// </remarks>
        public float SurfaceSignedDistanceMeters { get; }

        /// <inheritdoc />
        public bool Equals(FloodQueryResult other)
        {
            return IsInsideVolume == other.IsInsideVolume
                && IsSubmerged == other.IsSubmerged
                && SubmersionDepthMeters.Equals(other.SubmersionDepthMeters)
                && SurfacePoint.Equals(other.SurfacePoint)
                && SurfaceNormal.Equals(other.SurfaceNormal)
                && SurfaceSignedDistanceMeters.Equals(
                    other.SurfaceSignedDistanceMeters);
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
                SurfaceNormal,
                SurfaceSignedDistanceMeters);
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
