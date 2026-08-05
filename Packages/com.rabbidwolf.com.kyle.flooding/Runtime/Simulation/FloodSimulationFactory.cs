using System;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Shared construction of <see cref="FloodSimulation"/> from geometry.
    /// </summary>
    internal static class FloodSimulationFactory
    {
        private const float MinimumDimension = 0.01f;

        public static FloodSimulation Create(
            IFloodVolumeGeometry sourceGeometry,
            double volume)
        {
            if (sourceGeometry == null)
                throw new ArgumentNullException(nameof(sourceGeometry));

            var height = Math.Max(
                MinimumDimension,
                sourceGeometry.LocalBounds.size.y);
            return new FloodSimulation(
                GetEquivalentFloorArea(sourceGeometry),
                height,
                volume);
        }

        public static double GetEquivalentFloorArea(
            IFloodVolumeGeometry sourceGeometry)
        {
            if (sourceGeometry == null)
                throw new ArgumentNullException(nameof(sourceGeometry));

            var height = Math.Max(
                MinimumDimension,
                sourceGeometry.LocalBounds.size.y);
            return sourceGeometry.Capacity / height;
        }
    }
}
