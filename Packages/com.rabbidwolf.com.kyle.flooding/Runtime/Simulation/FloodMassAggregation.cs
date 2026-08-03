using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Combines independent mass contributors without mutating them.
    /// </summary>
    public static class FloodMassAggregation
    {
        /// <summary>
        /// Calculates total mass and its weighted world-space center.
        /// Contributors with zero mass do not affect the result.
        /// </summary>
        /// <param name="contributors">Contributors to combine.</param>
        /// <returns>The combined contribution, or an empty contribution.</returns>
        public static FloodMassContribution Combine(
            IReadOnlyList<IMassContributor> contributors)
        {
            if (contributors == null)
                throw new ArgumentNullException(nameof(contributors));

            double totalMass = 0d;
            double weightedX = 0d;
            double weightedY = 0d;
            double weightedZ = 0d;

            for (var index = 0; index < contributors.Count; index++)
            {
                var contributor = contributors[index];
                if (contributor == null)
                    continue;

                var mass = contributor.Mass;
                if (double.IsNaN(mass)
                    || double.IsInfinity(mass)
                    || mass < 0d)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(contributors),
                        mass,
                        "Contributor mass must be finite and non-negative.");
                }

                if (mass == 0d)
                    continue;

                var center = contributor.CenterOfMassWorld;
                totalMass += mass;
                weightedX += mass * center.x;
                weightedY += mass * center.y;
                weightedZ += mass * center.z;
            }

            if (totalMass == 0d)
                return FloodMassContribution.Empty;

            return new FloodMassContribution(
                totalMass,
                new Vector3(
                    (float)(weightedX / totalMass),
                    (float)(weightedY / totalMass),
                    (float)(weightedZ / totalMass)));
        }
    }
}
