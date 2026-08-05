using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Factual, presentation-only snapshot of water entering a
    /// <see cref="FloodVolume"/> from one provider.
    /// </summary>
    /// <remarks>
    /// Contains no profile-dependent strength and does not mutate simulation
    /// state. Normalized response curves are evaluated by
    /// <see cref="FloodIngressPresentationState"/>.
    /// </remarks>
    public readonly struct FloodIngressSample
    {
        /// <summary>
        /// Creates an ingress sample.
        /// </summary>
        /// <param name="providerId">
        /// Stable identity of the producing connection or source.
        /// </param>
        /// <param name="destinationVolume">
        /// Finite volume receiving the inflow.
        /// </param>
        /// <param name="worldPosition">
        /// World-space presentation anchor where water enters.
        /// </param>
        /// <param name="directionWorld">
        /// Unit direction water travels into
        /// <paramref name="destinationVolume"/>.
        /// </param>
        /// <param name="flowRateCubicMetersPerSecond">
        /// Non-negative effective inflow rate into the destination.
        /// </param>
        public FloodIngressSample(
            EntityId providerId,
            FloodVolume destinationVolume,
            Vector3 worldPosition,
            Vector3 directionWorld,
            float flowRateCubicMetersPerSecond)
        {
            ProviderId = providerId;
            DestinationVolume = destinationVolume;
            WorldPosition = worldPosition;
            DirectionWorld = directionWorld;
            FlowRateCubicMetersPerSecond = flowRateCubicMetersPerSecond;
        }

        /// <summary>
        /// Gets the stable identity of the producing connection or source.
        /// </summary>
        public EntityId ProviderId { get; }

        /// <summary>
        /// Gets the finite volume receiving the inflow.
        /// </summary>
        public FloodVolume DestinationVolume { get; }

        /// <summary>
        /// Gets the world-space presentation position where water enters.
        /// </summary>
        public Vector3 WorldPosition { get; }

        /// <summary>
        /// Gets the world-space direction water travels into
        /// <see cref="DestinationVolume"/>.
        /// </summary>
        public Vector3 DirectionWorld { get; }

        /// <summary>
        /// Gets the effective non-negative inflow rate in cubic meters per
        /// second.
        /// </summary>
        public float FlowRateCubicMetersPerSecond { get; }
    }
}
