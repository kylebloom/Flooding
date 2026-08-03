using System;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Immutable aggregate mass contribution in world space.
    /// </summary>
    public readonly struct FloodMassContribution : IEquatable<FloodMassContribution>
    {
        /// <summary>
        /// Creates an aggregate contribution.
        /// </summary>
        /// <param name="mass">Mass in kilograms.</param>
        /// <param name="centerOfMassWorld">Center of mass in world space.</param>
        public FloodMassContribution(double mass, Vector3 centerOfMassWorld)
        {
            if (double.IsNaN(mass)
                || double.IsInfinity(mass)
                || mass < 0d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(mass),
                    mass,
                    "Mass must be finite and non-negative.");
            }

            Mass = mass;
            CenterOfMassWorld = centerOfMassWorld;
        }

        /// <summary>
        /// Gets an empty contribution at the world origin.
        /// </summary>
        public static FloodMassContribution Empty { get; } =
            new FloodMassContribution(0d, Vector3.zero);

        /// <summary>
        /// Gets the aggregate mass in kilograms.
        /// </summary>
        public double Mass { get; }

        /// <summary>
        /// Gets the aggregate center of mass in world space.
        /// </summary>
        public Vector3 CenterOfMassWorld { get; }

        /// <inheritdoc />
        public bool Equals(FloodMassContribution other)
        {
            return Mass.Equals(other.Mass)
                && CenterOfMassWorld.Equals(other.CenterOfMassWorld);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is FloodMassContribution other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(Mass, CenterOfMassWorld);
        }

        /// <summary>
        /// Determines whether two contributions contain the same values.
        /// </summary>
        public static bool operator ==(
            FloodMassContribution left,
            FloodMassContribution right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two contributions contain different values.
        /// </summary>
        public static bool operator !=(
            FloodMassContribution left,
            FloodMassContribution right)
        {
            return !left.Equals(right);
        }
    }
}
