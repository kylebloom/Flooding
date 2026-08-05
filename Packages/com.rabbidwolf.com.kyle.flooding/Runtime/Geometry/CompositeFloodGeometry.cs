using System.Collections.Generic;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Host for region union geometry. Selects a concrete
    /// <see cref="IRegionUnionStrategy"/> without exposing strategy types on
    /// <see cref="FloodRegion"/>.
    /// </summary>
    public static class CompositeFloodGeometry
    {
        /// <summary>
        /// Builds composite geometry for the supplied region members.
        /// </summary>
        public static bool TryCreate(
            FloodRegion region,
            IReadOnlyList<FloodVolume> members,
            out IFloodVolumeGeometry geometry,
            out string message)
        {
            geometry = null;
            message = null;

            if (region == null)
            {
                message = "FloodRegion is required.";
                return false;
            }

            if (members == null || members.Count == 0)
            {
                message = "Composite geometry requires at least one member.";
                return false;
            }

            if (members.Count == 1)
            {
                var member = members[0];
                geometry = member != null ? member.Geometry : null;
                if (geometry == null)
                {
                    message = "Single member has invalid geometry.";
                    return false;
                }

                message = string.Empty;
                return true;
            }

            if (members.Count == 2
                && TwoBoxAnalyticUnionStrategy.CanHandle(members))
            {
                var strategy = new TwoBoxAnalyticUnionStrategy();
                return strategy.TryBuild(
                    region.transform,
                    members,
                    out geometry,
                    out message);
            }

            message =
                $"No region union strategy supports {members.Count} members yet. "
                + "Phase B supports exactly two rectangular members via "
                + "TwoBoxAnalyticUnionStrategy; general occupancy bake is planned.";
            return false;
        }
    }
}
