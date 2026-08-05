using System.Collections.Generic;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Region-union strategy backed by an Editor-baked
    /// <see cref="FloodRegionData"/> occupancy asset.
    /// </summary>
    public sealed class RegionOccupancyUnionStrategy : IRegionUnionStrategy
    {
        private readonly FloodRegionData bakedData;

        /// <summary>
        /// Creates a strategy that consumes the supplied bake asset.
        /// </summary>
        public RegionOccupancyUnionStrategy(FloodRegionData bakedData)
        {
            this.bakedData = bakedData;
        }

        /// <summary>
        /// Returns whether the region has a usable bake for this strategy.
        /// </summary>
        public static bool CanHandle(FloodRegion region)
        {
            return region != null
                && region.BakedRegionData != null
                && region.BakedRegionData.IsUsable;
        }

        /// <inheritdoc />
        public bool TryBuild(
            Transform regionTransform,
            IReadOnlyList<FloodVolume> members,
            out IFloodVolumeGeometry geometry,
            out string message)
        {
            geometry = null;
            message = null;

            if (bakedData == null || !bakedData.IsUsable)
            {
                message =
                    "RegionOccupancyUnionStrategy requires a usable "
                    + "FloodRegionData bake asset.";
                return false;
            }

            if (regionTransform == null)
            {
                message = "Region transform is required.";
                return false;
            }

            if (members == null || members.Count < 2)
            {
                message =
                    "Region occupancy union requires at least two members.";
                return false;
            }

            for (var index = 0; index < members.Count; index++)
            {
                if (members[index] == null)
                {
                    message = $"Member slot {index} is unassigned.";
                    return false;
                }

                if (members[index].Geometry == null)
                {
                    message =
                        $"FloodVolume '{members[index].name}' has invalid geometry.";
                    return false;
                }
            }

            geometry = new BakedFloodGeometry(bakedData);
            message = string.Empty;
            return true;
        }
    }
}
