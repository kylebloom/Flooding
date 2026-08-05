using System;

namespace Kyle.Flooding
{
    /// <summary>
    /// Immutable result of one pressure-head flow calculation.
    /// </summary>
    public readonly struct FloodFlowResult
    {
        internal FloodFlowResult(
            double signedFlowRate,
            double submergedOpeningArea,
            double pressureHeadDifference)
        {
            SignedFlowRate = signedFlowRate;
            SubmergedOpeningArea = submergedOpeningArea;
            PressureHeadDifference = pressureHeadDifference;
        }

        /// <summary>
        /// Gets signed flow in cubic meters per second. Positive values flow
        /// from side A to B; negative values flow from B to A.
        /// </summary>
        public double SignedFlowRate { get; }

        /// <summary>
        /// Gets the source-side effective submerged opening area in square
        /// meters after applying the open-fraction aperture multiplier.
        /// </summary>
        public double SubmergedOpeningArea { get; }

        /// <summary>
        /// Gets the signed pressure-head difference in meters, evaluated at the
        /// submerged-opening centroid. Positive values indicate greater head on
        /// side A.
        /// </summary>
        public double PressureHeadDifference { get; }

        /// <summary>
        /// Gets whether this result requests non-zero flow.
        /// </summary>
        public bool IsFlowing => SignedFlowRate != 0d;
    }

    /// <summary>
    /// Calculates simplified bidirectional orifice flow for a vertical
    /// rectangular opening.
    /// </summary>
    public static class FloodFlowCalculator
    {
        /// <summary>
        /// Calculates signed pressure-driven flow from opening-bottom heads.
        /// </summary>
        /// <remarks>
        /// Submerged opening area uses the greater opening-bottom depth, then
        /// multiplies by <paramref name="openFraction"/> so only the available
        /// aperture participates in flow. Pressure head for orifice flow is
        /// evaluated at the centroid of the authored submerged portion (half
        /// the submerged height above the opening bottom). Differences within
        /// <see cref="FloodFluidTolerances.PressureHead"/> produce no flow.
        /// </remarks>
        /// <param name="pressureHeadA">
        /// Water depth above the opening bottom on side A, in meters.
        /// </param>
        /// <param name="pressureHeadB">
        /// Water depth above the opening bottom on side B, in meters.
        /// </param>
        /// <param name="openingWidth">Opening width in meters.</param>
        /// <param name="openingHeight">Opening height in meters.</param>
        /// <param name="dischargeCoefficient">
        /// Dimensionless coefficient from zero to one.
        /// </param>
        /// <param name="gravityMagnitude">
        /// Gravity magnitude in meters per second squared.
        /// </param>
        /// <param name="openFraction">
        /// Effective-aperture multiplier from zero to one. Zero is
        /// hydraulically closed; one uses the full submerged aperture.
        /// </param>
        /// <returns>The signed flow rate and diagnostic values.</returns>
        public static FloodFlowResult Calculate(
            double pressureHeadA,
            double pressureHeadB,
            double openingWidth,
            double openingHeight,
            double dischargeCoefficient,
            double gravityMagnitude,
            double openFraction = 1d)
        {
            EnsureFinite(pressureHeadA, nameof(pressureHeadA));
            EnsureFinite(pressureHeadB, nameof(pressureHeadB));
            EnsureFiniteNonNegative(openingWidth, nameof(openingWidth));
            EnsureFiniteNonNegative(openingHeight, nameof(openingHeight));
            EnsureFiniteNonNegative(gravityMagnitude, nameof(gravityMagnitude));
            EnsureFinite(dischargeCoefficient, nameof(dischargeCoefficient));
            EnsureFinite(openFraction, nameof(openFraction));

            if (dischargeCoefficient < 0d || dischargeCoefficient > 1d)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(dischargeCoefficient));
            }

            if (openFraction < 0d || openFraction > 1d)
                throw new ArgumentOutOfRangeException(nameof(openFraction));

            var clampedHeadA = Math.Max(0d, pressureHeadA);
            var clampedHeadB = Math.Max(0d, pressureHeadB);
            var sourceHead = Math.Max(clampedHeadA, clampedHeadB);
            var submergedHeight = Math.Min(sourceHead, openingHeight);
            var submergedArea = openingWidth * submergedHeight;
            var effectiveSubmergedArea = submergedArea * openFraction;

            if (double.IsInfinity(effectiveSubmergedArea))
                throw new ArgumentOutOfRangeException(nameof(openingWidth));

            var centroidOffset = submergedHeight * 0.5d;
            var centroidHeadA = Math.Max(0d, clampedHeadA - centroidOffset);
            var centroidHeadB = Math.Max(0d, clampedHeadB - centroidOffset);
            var headDifference = centroidHeadA - centroidHeadB;

            if (Math.Abs(headDifference) <= FloodFluidTolerances.PressureHead)
                headDifference = 0d;

            if (
                headDifference == 0d
                || effectiveSubmergedArea <= 0d
                || dischargeCoefficient <= 0d
                || gravityMagnitude <= 0d)
            {
                return new FloodFlowResult(
                    0d,
                    effectiveSubmergedArea,
                    headDifference);
            }

            var flowMagnitude =
                dischargeCoefficient
                * effectiveSubmergedArea
                * Math.Sqrt(
                    2d
                    * gravityMagnitude
                    * Math.Abs(headDifference));

            if (double.IsInfinity(flowMagnitude))
                throw new ArgumentOutOfRangeException(nameof(openingWidth));

            return new FloodFlowResult(
                headDifference > 0d
                    ? flowMagnitude
                    : -flowMagnitude,
                effectiveSubmergedArea,
                headDifference);
        }

        private static void EnsureFiniteNonNegative(
            double value,
            string parameterName)
        {
            EnsureFinite(value, parameterName);

            if (value < 0d)
                throw new ArgumentOutOfRangeException(parameterName);
        }

        private static void EnsureFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
