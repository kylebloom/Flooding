using System;

namespace Kyle.Flooding
{
    /// <summary>
    /// Numerical tolerances for fluid-boundary comparison and flow deadbands.
    /// </summary>
    public static class FloodFluidTolerances
    {
        /// <summary>
        /// Absolute density match tolerance in kilograms per cubic meter.
        /// </summary>
        public const double DensityAbsolute = 0.001d;

        /// <summary>
        /// Relative density match tolerance.
        /// </summary>
        public const double DensityRelative = 0.000001d;

        /// <summary>
        /// Absolute pressure-head deadband in meters. Differences at or below
        /// this magnitude produce no flow.
        /// </summary>
        public const double PressureHead = 0.000001d;

        /// <summary>
        /// Returns whether two densities match within absolute and relative
        /// tolerances.
        /// </summary>
        public static bool DensitiesMatch(double a, double b)
        {
            return NearlyEqual(
                a,
                b,
                DensityAbsolute,
                DensityRelative);
        }

        /// <summary>
        /// Combined absolute and relative equality test.
        /// </summary>
        public static bool NearlyEqual(
            double a,
            double b,
            double absoluteTolerance,
            double relativeTolerance)
        {
            if (
                double.IsNaN(a)
                || double.IsNaN(b)
                || double.IsInfinity(a)
                || double.IsInfinity(b)
                || absoluteTolerance < 0d
                || relativeTolerance < 0d)
            {
                return false;
            }

            var difference = Math.Abs(a - b);

            if (difference <= absoluteTolerance)
                return true;

            var scale = Math.Max(Math.Abs(a), Math.Abs(b));
            return difference <= scale * relativeTolerance;
        }
    }
}
