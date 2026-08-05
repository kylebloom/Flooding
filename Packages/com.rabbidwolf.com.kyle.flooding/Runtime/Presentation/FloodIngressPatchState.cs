using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Presentation-only state for one provider-owned local ingress patch.
    /// </summary>
    /// <remarks>
    /// Does not store authoritative water volume. Flow impulse / visual weight
    /// influences spread only and must not be treated as cubic meters.
    /// </remarks>
    public struct FloodIngressPatchState
    {
        public EntityId ProviderId;
        public Vector3 CenterWorld;
        public Vector3 FloorNormalWorld;
        public Vector3 DirectionWorld;

        /// <summary>
        /// Floor-projected spread direction used for elongated footprints.
        /// </summary>
        public Vector3 SpreadDirectionWorld;

        /// <summary>
        /// Scalar radius used by lifecycle growth (before directional stretch).
        /// </summary>
        public float CurrentRadius;

        public float TargetRadius;
        public float VisualDepth;
        public float AgeSeconds;
        public float PhaseAgeSeconds;
        public FloodIngressPatchPhase Phase;
        public float HandoffFraction;
        public float FlowImpulse;
        public float Strength;
        public float FlowRateCubicMetersPerSecond;

        /// <summary>
        /// Current directional elongation factor (0 = round, higher = elongated).
        /// </summary>
        public float DirectionalStretch;

        /// <summary>
        /// Major-axis visual radius in meters after stretch.
        /// </summary>
        public float MajorRadius;

        /// <summary>
        /// Minor-axis visual radius in meters after stretch.
        /// </summary>
        public float MinorRadius;

        public bool IsActive => Phase != FloodIngressPatchPhase.Inactive;

        public float LocalOpacity =>
            IsActive ? Mathf.Clamp01(1f - HandoffFraction) * Strength : 0f;
    }
}
