using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Immutable tick-start facts for one fluid boundary endpoint.
    /// </summary>
    public readonly struct FluidBoundarySnapshot
    {
        internal FluidBoundarySnapshot(
            FluidBoundaryId boundaryId,
            FloodSimulationManager owner,
            Plane surfacePlane,
            double densityKgPerCubicMeter,
            bool hasFiniteSupply,
            double availableVolume,
            bool hasFiniteCapacity,
            double remainingCapacity,
            bool acceptsCommits,
            bool isEnabled)
        {
            BoundaryId = boundaryId;
            Owner = owner;
            SurfacePlane = surfacePlane;
            DensityKgPerCubicMeter = densityKgPerCubicMeter;
            HasFiniteSupply = hasFiniteSupply;
            AvailableVolume = availableVolume;
            HasFiniteCapacity = hasFiniteCapacity;
            RemainingCapacity = remainingCapacity;
            AcceptsCommits = acceptsCommits;
            IsEnabled = isEnabled;
        }

        /// <summary>
        /// Gets the stable boundary identity.
        /// </summary>
        public FluidBoundaryId BoundaryId { get; }

        /// <summary>
        /// Gets the manager that owns this boundary for the tick.
        /// </summary>
        public FloodSimulationManager Owner { get; }

        /// <summary>
        /// Gets the world-space fluid surface plane. The negative half-space is
        /// submerged.
        /// </summary>
        public Plane SurfacePlane { get; }

        /// <summary>
        /// Gets fluid density in kilograms per cubic meter.
        /// </summary>
        public double DensityKgPerCubicMeter { get; }

        /// <summary>
        /// Gets whether supply is limited by <see cref="AvailableVolume"/>.
        /// </summary>
        public bool HasFiniteSupply { get; }

        /// <summary>
        /// Gets available supply volume in cubic meters when
        /// <see cref="HasFiniteSupply"/> is true. Otherwise unused.
        /// </summary>
        public double AvailableVolume { get; }

        /// <summary>
        /// Gets whether receiving capacity is limited by
        /// <see cref="RemainingCapacity"/>.
        /// </summary>
        public bool HasFiniteCapacity { get; }

        /// <summary>
        /// Gets remaining capacity in cubic meters when
        /// <see cref="HasFiniteCapacity"/> is true. Otherwise unused.
        /// </summary>
        public double RemainingCapacity { get; }

        /// <summary>
        /// Gets whether the manager may later commit a finite volume delta to
        /// this endpoint through its registration table.
        /// </summary>
        public bool AcceptsCommits { get; }

        /// <summary>
        /// Gets whether the boundary was enabled at capture time.
        /// </summary>
        public bool IsEnabled { get; }
    }
}
