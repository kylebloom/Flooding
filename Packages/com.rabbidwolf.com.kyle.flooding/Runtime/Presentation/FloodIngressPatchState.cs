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
        /// <summary>
        /// Gets or sets the producing connection or source identity.
        /// </summary>
        public EntityId ProviderId;

        /// <summary>
        /// Gets or sets the world-space patch center on the presentation floor.
        /// </summary>
        public Vector3 CenterWorld;

        /// <summary>
        /// Gets or sets the floor-aligned normal used for patch orientation.
        /// </summary>
        public Vector3 FloorNormalWorld;

        /// <summary>
        /// Gets or sets the latest ingress travel direction into the volume.
        /// </summary>
        public Vector3 DirectionWorld;

        /// <summary>
        /// Gets or sets the current visual radius in meters.
        /// </summary>
        public float CurrentRadius;

        /// <summary>
        /// Gets or sets the target visual radius in meters.
        /// </summary>
        public float TargetRadius;

        /// <summary>
        /// Gets or sets the shallow visual depth scalar in meters.
        /// </summary>
        public float VisualDepth;

        /// <summary>
        /// Gets or sets elapsed active lifetime in seconds.
        /// </summary>
        public float AgeSeconds;

        /// <summary>
        /// Gets or sets seconds spent in the current phase.
        /// </summary>
        public float PhaseAgeSeconds;

        /// <summary>
        /// Gets or sets the lifecycle phase.
        /// </summary>
        public FloodIngressPatchPhase Phase;

        /// <summary>
        /// Gets or sets bulk handoff fraction (0 = fully local, 1 = fully bulk).
        /// </summary>
        public float HandoffFraction;

        /// <summary>
        /// Gets or sets presentation-only accumulated flow impulse used to drive
        /// spread. Not conserved water volume.
        /// </summary>
        public float FlowImpulse;

        /// <summary>
        /// Gets or sets the latest 0–1 presentation strength from profile curves.
        /// </summary>
        public float Strength;

        /// <summary>
        /// Gets or sets the latest sampled inflow rate in cubic meters per second.
        /// </summary>
        public float FlowRateCubicMetersPerSecond;

        /// <summary>
        /// Gets whether this slot currently owns an active presentation patch.
        /// </summary>
        public bool IsActive => Phase != FloodIngressPatchPhase.Inactive;

        /// <summary>
        /// Gets local opacity contribution after handoff (1 - handoff).
        /// </summary>
        public float LocalOpacity =>
            IsActive ? Mathf.Clamp01(1f - HandoffFraction) * Strength : 0f;
    }
}
