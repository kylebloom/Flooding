using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Reports a non-negative mass and its center in world space.
    /// </summary>
    public interface IMassContributor
    {
        /// <summary>
        /// Gets the contributed mass in kilograms.
        /// </summary>
        double Mass { get; }

        /// <summary>
        /// Gets the contributed center of mass in world space.
        /// </summary>
        Vector3 CenterOfMassWorld { get; }
    }
}
