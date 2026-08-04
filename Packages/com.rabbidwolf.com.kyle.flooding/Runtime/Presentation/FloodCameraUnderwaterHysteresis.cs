namespace Kyle.Flooding
{
    /// <summary>
    /// Pure underwater enter/exit hysteresis for camera presentation.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="FloodQueryResult.SurfaceSignedDistanceMeters"/> sign
    /// convention: positive above water, negative below. Thresholds are
    /// compared directly against that signed distance.
    /// </remarks>
    public static class FloodCameraUnderwaterHysteresis
    {
        /// <summary>
        /// Default signed-distance threshold for entering water (meters).
        /// Slightly below the surface to avoid flicker on the plane.
        /// </summary>
        public const float DefaultEnterWaterThresholdMeters = -0.02f;

        /// <summary>
        /// Default signed-distance threshold for exiting water (meters).
        /// Slightly above the surface to avoid flicker on the plane.
        /// </summary>
        public const float DefaultExitWaterThresholdMeters = 0.02f;

        /// <summary>
        /// Evaluates whether the camera should be considered underwater after
        /// applying enter/exit hysteresis.
        /// </summary>
        /// <param name="currentlyUnderwater">
        /// Current latched underwater presentation state.
        /// </param>
        /// <param name="isInsideFloodVolume">
        /// Whether the viewpoint is inside the active flood compartment.
        /// Underwater is never true while outside.
        /// </param>
        /// <param name="surfaceSignedDistanceMeters">
        /// Signed distance to the authoritative surface plane (positive above).
        /// </param>
        /// <param name="enterWaterThresholdMeters">
        /// Enter water when currently dry and
        /// <paramref name="surfaceSignedDistanceMeters"/> is less than or equal
        /// to this value. Typically slightly negative.
        /// </param>
        /// <param name="exitWaterThresholdMeters">
        /// Exit water when currently underwater and
        /// <paramref name="surfaceSignedDistanceMeters"/> is greater than or
        /// equal to this value. Typically slightly positive.
        /// </param>
        /// <returns>The next latched underwater state.</returns>
        public static bool Evaluate(
            bool currentlyUnderwater,
            bool isInsideFloodVolume,
            float surfaceSignedDistanceMeters,
            float enterWaterThresholdMeters,
            float exitWaterThresholdMeters)
        {
            if (!isInsideFloodVolume)
                return false;

            if (currentlyUnderwater)
                return surfaceSignedDistanceMeters < exitWaterThresholdMeters;

            return surfaceSignedDistanceMeters <= enterWaterThresholdMeters;
        }
    }
}
