using System.Collections.Generic;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Sticky active-volume selection for camera presentation consumers.
    /// </summary>
    /// <remarks>
    /// Overlapping <see cref="FloodVolume"/> compartments are ambiguous and are
    /// not physically merged. Selection is sticky while the current active
    /// volume still contains the viewpoint, independent of underwater state.
    /// </remarks>
    public static class FloodCameraVolumeSelection
    {
        /// <summary>
        /// Selects the active flood volume for a viewpoint.
        /// </summary>
        /// <param name="currentActive">
        /// Volume that was active on the previous evaluation, or null.
        /// </param>
        /// <param name="registeredVolumes">
        /// Candidate volumes in manager registration order.
        /// </param>
        /// <param name="worldPoint">Viewpoint in world space.</param>
        /// <returns>
        /// The volume to keep or newly select, or null when none contain the
        /// viewpoint.
        /// </returns>
        public static FloodVolume SelectActiveVolume(
            FloodVolume currentActive,
            IReadOnlyList<FloodVolume> registeredVolumes,
            Vector3 worldPoint)
        {
            if (currentActive != null && currentActive.ContainsPoint(worldPoint))
                return currentActive;

            if (registeredVolumes == null || registeredVolumes.Count == 0)
                return null;

            FloodVolume selected = null;
            var selectedIsSubmerged = false;
            var selectedSubmersionDepth = float.NegativeInfinity;

            for (var i = 0; i < registeredVolumes.Count; i++)
            {
                var candidate = registeredVolumes[i];
                if (candidate == null)
                    continue;

                var query = candidate.QueryPoint(worldPoint);
                if (!query.IsInsideVolume)
                    continue;

                if (selected == null)
                {
                    selected = candidate;
                    selectedIsSubmerged = query.IsSubmerged;
                    selectedSubmersionDepth = query.SubmersionDepthMeters;
                    continue;
                }

                if (query.IsSubmerged == selectedIsSubmerged)
                {
                    if (!query.IsSubmerged)
                        continue;

                    if (query.SubmersionDepthMeters > selectedSubmersionDepth)
                    {
                        selected = candidate;
                        selectedSubmersionDepth = query.SubmersionDepthMeters;
                    }

                    continue;
                }

                if (!query.IsSubmerged || selectedIsSubmerged)
                    continue;

                selected = candidate;
                selectedIsSubmerged = true;
                selectedSubmersionDepth = query.SubmersionDepthMeters;
            }

            return selected;
        }
    }
}
