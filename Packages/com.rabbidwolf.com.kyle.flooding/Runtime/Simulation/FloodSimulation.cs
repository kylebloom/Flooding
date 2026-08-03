using System;

namespace Kyle.Flooding
{
    /// <summary>
    /// Pure C# flooding simulation.
    /// Does not depend on Unity scene objects.
    /// </summary>
    public sealed class FloodSimulation
    {
        public double FloorArea { get; }

        public double MaximumHeight { get; }

        public double MaximumVolume => FloorArea * MaximumHeight;

        public double CurrentVolume { get; private set; }

        public double CurrentHeight =>
            FloorArea <= 0
                ? 0
                : CurrentVolume / FloorArea;

        public double FillPercentage =>
            MaximumVolume <= 0
                ? 0
                : CurrentVolume / MaximumVolume;

        public bool IsFull =>
            CurrentVolume >= MaximumVolume;

        public bool IsEmpty =>
            CurrentVolume <= 0;

        public FloodSimulation(
            double floorArea,
            double maximumHeight,
            double initialVolume = 0)
        {
            if (!double.IsFinite(floorArea) || floorArea <= 0)
                throw new ArgumentOutOfRangeException(nameof(floorArea));

            if (!double.IsFinite(maximumHeight) || maximumHeight <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumHeight));

            if (!double.IsFinite(initialVolume))
                throw new ArgumentOutOfRangeException(nameof(initialVolume));

            if (floorArea > double.MaxValue / maximumHeight)
                throw new ArgumentOutOfRangeException(nameof(maximumHeight));

            FloorArea = floorArea;
            MaximumHeight = maximumHeight;

            CurrentVolume = Math.Clamp(
                initialVolume,
                0,
                MaximumVolume);
        }

        public VolumeChangeResult AddVolume(double cubicMeters)
        {
            EnsureFinite(cubicMeters, nameof(cubicMeters));

            if (cubicMeters <= 0)
                return CreateNoChangeResult();

            return ApplySignedChange(cubicMeters);
        }

        public VolumeChangeResult RemoveVolume(double cubicMeters)
        {
            EnsureFinite(cubicMeters, nameof(cubicMeters));

            if (cubicMeters <= 0)
                return CreateNoChangeResult();

            return ApplySignedChange(-cubicMeters);
        }

        public VolumeChangeResult SetVolume(double cubicMeters)
        {
            EnsureFinite(cubicMeters, nameof(cubicMeters));

            var targetVolume = Math.Clamp(
                cubicMeters,
                0,
                MaximumVolume);

            return ApplySignedChange(cubicMeters - CurrentVolume, targetVolume);
        }

        public VolumeChangeResult Step(
            double deltaTime,
            double inflowRate,
            double outflowRate)
        {
            EnsureFinite(deltaTime, nameof(deltaTime));
            EnsureFinite(inflowRate, nameof(inflowRate));
            EnsureFinite(outflowRate, nameof(outflowRate));

            if (deltaTime <= 0)
                return CreateNoChangeResult();

            if (inflowRate < 0)
                throw new ArgumentOutOfRangeException(nameof(inflowRate));

            if (outflowRate < 0)
                throw new ArgumentOutOfRangeException(nameof(outflowRate));

            var netFlowRate = inflowRate - outflowRate;
            var volumeChange = netFlowRate * deltaTime;

            EnsureFinite(volumeChange, nameof(volumeChange));

            return ApplySignedChange(volumeChange);
        }

        private VolumeChangeResult ApplySignedChange(double requestedChange)
        {
            double targetVolume;

            if (requestedChange >= 0)
            {
                targetVolume =
                    CurrentVolume
                    + Math.Min(
                        requestedChange,
                        MaximumVolume - CurrentVolume);
            }
            else
            {
                targetVolume =
                    CurrentVolume
                    - Math.Min(
                        -requestedChange,
                        CurrentVolume);
            }

            return ApplySignedChange(requestedChange, targetVolume);
        }

        private VolumeChangeResult ApplySignedChange(
            double requestedChange,
            double targetVolume)
        {
            var previousVolume = CurrentVolume;
            CurrentVolume = targetVolume;

            return new VolumeChangeResult(
                requestedChange,
                CurrentVolume - previousVolume,
                previousVolume,
                CurrentVolume);
        }

        private VolumeChangeResult CreateNoChangeResult()
        {
            return new VolumeChangeResult(
                requestedChange: 0d,
                appliedChange: 0d,
                previousVolume: CurrentVolume,
                currentVolume: CurrentVolume);
        }

        private static void EnsureFinite(double value, string parameterName)
        {
            if (!double.IsFinite(value))
                throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}