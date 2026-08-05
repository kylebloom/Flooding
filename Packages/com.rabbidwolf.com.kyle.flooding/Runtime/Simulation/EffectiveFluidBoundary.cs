using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Resolves the independently simulated fluid boundary that owns water
    /// state for a volume reference.
    /// </summary>
    /// <remarks>
    /// Sources, sinks, connections, and gameplay mutations may author against a
    /// <see cref="FloodVolume"/>. When that volume is a member of a
    /// <see cref="FloodRegion"/>, hydraulic mutations apply to the region.
    /// Spatial attachment (for example a source Transform inside Room A) may
    /// remain on the volume.
    /// </remarks>
    public static class EffectiveFluidBoundary
    {
        /// <summary>
        /// Resolves the effective finite boundary for a flood volume.
        /// </summary>
        /// <returns>
        /// The owning <see cref="FloodRegion"/> when the volume is a region
        /// member; otherwise the volume itself. Returns null when
        /// <paramref name="volume"/> is null.
        /// </returns>
        public static IFluidBoundary Resolve(FloodVolume volume)
        {
            if (volume == null)
                return null;

            var region = volume.OwningRegion;
            return region != null ? region : volume;
        }

        /// <summary>
        /// Resolves the effective boundary for any fluid-boundary component.
        /// </summary>
        public static IFluidBoundary Resolve(IFluidBoundary boundary)
        {
            if (boundary is FloodVolume volume)
                return Resolve(volume);

            return boundary;
        }

        /// <summary>
        /// Gets the owning region when <paramref name="volume"/> is a member;
        /// otherwise null.
        /// </summary>
        public static FloodRegion ResolveRegion(FloodVolume volume)
        {
            return volume != null ? volume.OwningRegion : null;
        }

        /// <summary>
        /// Resolves the single manager commit-participant volume for a target.
        /// Multi-member regions route all mutations through the primary member
        /// to avoid double-counting shared water.
        /// </summary>
        public static FloodVolume ResolveCommitVolume(FloodVolume volume)
        {
            if (volume == null)
                return null;

            var region = volume.OwningRegion;
            if (region == null || region.BoundMembers.Count == 0)
                return volume;

            return region.BoundMembers[0];
        }
    }
}
