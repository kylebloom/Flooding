using System;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Shared intensity mapping for optional flow and fill presentation.
    /// </summary>
    public static class FloodPresentationUtility
    {
        /// <summary>
        /// Absolute flow rate at or below this value is treated as idle.
        /// </summary>
        public const double IdleFlowRate = 0.0001d;

        /// <summary>
        /// Maps an absolute flow rate onto a 0–1 intensity using low and high
        /// band thresholds in cubic meters per second.
        /// </summary>
        public static float FlowIntensity(
            double absoluteFlowRateCubicMetersPerSecond,
            float lowThreshold,
            float highThreshold)
        {
            var rate = Math.Max(0d, absoluteFlowRateCubicMetersPerSecond);
            var low = Math.Max(0f, lowThreshold);
            var high = Math.Max(low, highThreshold);

            if (rate <= IdleFlowRate || high <= 0f)
                return 0f;

            if (rate <= low)
                return (float)(rate / Math.Max(low, IdleFlowRate)) * 0.33f;

            if (rate >= high)
                return 1f;

            var mid = (float)((rate - low) / Math.Max(high - low, IdleFlowRate));
            return 0.33f + (mid * 0.67f);
        }

        /// <summary>
        /// Clamps fill percentage into a 0–1 presentation intensity.
        /// </summary>
        public static float FillIntensity(double fillPercentage)
        {
            if (double.IsNaN(fillPercentage) || double.IsInfinity(fillPercentage))
                return 0f;

            return Mathf.Clamp01((float)fillPercentage);
        }

        /// <summary>
        /// Returns whether a signed flow rate should be considered active.
        /// </summary>
        public static bool IsFlowing(double signedFlowRateCubicMetersPerSecond)
        {
            return Math.Abs(signedFlowRateCubicMetersPerSecond) > IdleFlowRate;
        }
    }
}
