using System;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Immutable snapshot of the public state of one floodable compartment.
    /// </summary>
    public readonly struct FloodState : IEquatable<FloodState>
    {
        internal FloodState(
            double volume,
            double capacity,
            double height,
            double fillPercentage,
            bool isEmpty,
            bool isFull,
            Plane surfacePlane,
            double waterMass,
            Vector3 waterCenterOfMassWorld)
        {
            Volume = volume;
            Capacity = capacity;
            Height = height;
            FillPercentage = fillPercentage;
            IsEmpty = isEmpty;
            IsFull = isFull;
            SurfacePlane = surfacePlane;
            WaterMass = waterMass;
            WaterCenterOfMassWorld = waterCenterOfMassWorld;
        }

        /// <summary>
        /// Gets the current water volume in cubic meters.
        /// </summary>
        public double Volume { get; }

        /// <summary>
        /// Gets the compartment capacity in cubic meters.
        /// </summary>
        public double Capacity { get; }

        /// <summary>
        /// Gets the equivalent level-fill height in meters. For tilted
        /// surfaces, use SurfacePlane for actual surface position.
        /// </summary>
        public double Height { get; }

        /// <summary>
        /// Gets the normalized fill percentage from zero to one.
        /// </summary>
        public double FillPercentage { get; }

        /// <summary>
        /// Gets whether the compartment contains no water.
        /// </summary>
        public bool IsEmpty { get; }

        /// <summary>
        /// Gets whether the compartment is at capacity.
        /// </summary>
        public bool IsFull { get; }

        /// <summary>
        /// Gets the current water surface plane in world space.
        /// </summary>
        public Plane SurfacePlane { get; }

        /// <summary>
        /// Gets the water mass in kilograms.
        /// </summary>
        public double WaterMass { get; }

        /// <summary>
        /// Gets the water center of mass in world space.
        /// </summary>
        public Vector3 WaterCenterOfMassWorld { get; }

        /// <inheritdoc />
        public bool Equals(FloodState other)
        {
            return Volume.Equals(other.Volume)
                && Capacity.Equals(other.Capacity)
                && Height.Equals(other.Height)
                && FillPercentage.Equals(other.FillPercentage)
                && IsEmpty == other.IsEmpty
                && IsFull == other.IsFull
                && SurfacePlane.Equals(other.SurfacePlane)
                && WaterMass.Equals(other.WaterMass)
                && WaterCenterOfMassWorld.Equals(other.WaterCenterOfMassWorld);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is FloodState other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(
                HashCode.Combine(
                    Volume,
                    Capacity,
                    Height,
                    FillPercentage,
                    IsEmpty,
                    IsFull,
                    SurfacePlane,
                    WaterMass),
                WaterCenterOfMassWorld);
        }

        /// <summary>
        /// Determines whether two snapshots contain the same values.
        /// </summary>
        public static bool operator ==(FloodState left, FloodState right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two snapshots contain different values.
        /// </summary>
        public static bool operator !=(FloodState left, FloodState right)
        {
            return !left.Equals(right);
        }
    }
}
