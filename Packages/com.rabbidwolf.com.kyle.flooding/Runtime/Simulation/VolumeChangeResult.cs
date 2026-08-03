using System;

namespace Kyle.Flooding
{
    /// <summary>
    /// Describes a requested and applied change to a flood volume.
    /// </summary>
    public readonly struct VolumeChangeResult : IEquatable<VolumeChangeResult>
    {
        internal VolumeChangeResult(
            double requestedChange,
            double appliedChange,
            double previousVolume,
            double currentVolume)
        {
            RequestedChange = requestedChange;
            AppliedChange = appliedChange;
            PreviousVolume = previousVolume;
            CurrentVolume = currentVolume;
            RejectedVolume = Math.Max(
                0d,
                Math.Abs(requestedChange) - Math.Abs(appliedChange));
        }

        /// <summary>
        /// Gets the signed requested change in cubic meters.
        /// Positive values add water and negative values remove water.
        /// </summary>
        public double RequestedChange { get; }

        /// <summary>
        /// Gets the signed applied change in cubic meters.
        /// Positive values add water and negative values remove water.
        /// </summary>
        public double AppliedChange { get; }

        /// <summary>
        /// Gets the unapplied magnitude in cubic meters.
        /// </summary>
        public double RejectedVolume { get; }

        /// <summary>
        /// Gets the volume before the operation, in cubic meters.
        /// </summary>
        public double PreviousVolume { get; }

        /// <summary>
        /// Gets the volume after the operation, in cubic meters.
        /// </summary>
        public double CurrentVolume { get; }

        /// <summary>
        /// Gets whether the operation changed the stored volume.
        /// </summary>
        public bool Changed => AppliedChange != 0d;

        /// <inheritdoc />
        public bool Equals(VolumeChangeResult other)
        {
            return RequestedChange.Equals(other.RequestedChange)
                && AppliedChange.Equals(other.AppliedChange)
                && RejectedVolume.Equals(other.RejectedVolume)
                && PreviousVolume.Equals(other.PreviousVolume)
                && CurrentVolume.Equals(other.CurrentVolume);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is VolumeChangeResult other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(
                RequestedChange,
                AppliedChange,
                RejectedVolume,
                PreviousVolume,
                CurrentVolume);
        }

        /// <summary>
        /// Determines whether two results contain the same values.
        /// </summary>
        public static bool operator ==(
            VolumeChangeResult left,
            VolumeChangeResult right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two results contain different values.
        /// </summary>
        public static bool operator !=(
            VolumeChangeResult left,
            VolumeChangeResult right)
        {
            return !left.Equals(right);
        }
    }
}
