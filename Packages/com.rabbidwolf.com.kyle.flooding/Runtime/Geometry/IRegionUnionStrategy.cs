using System.Collections.Generic;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Pluggable strategy that builds region-local union geometry from member
    /// volumes without double-counting overlapping capacity.
    /// </summary>
    public interface IRegionUnionStrategy
    {
        /// <summary>
        /// Attempts to build immutable union geometry in region-local space.
        /// </summary>
        bool TryBuild(
            Transform regionTransform,
            IReadOnlyList<FloodVolume> members,
            out IFloodVolumeGeometry geometry,
            out string message);
    }
}
