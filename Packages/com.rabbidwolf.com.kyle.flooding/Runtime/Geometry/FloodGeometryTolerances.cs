namespace Kyle.Flooding
{
    /// <summary>
    /// Numerical tolerances used by deterministic container geometry queries.
    /// Values are expressed in SI units unless dimensionless.
    /// </summary>
    public static class FloodGeometryTolerances
    {
        /// <summary>
        /// Minimum accepted authored dimension in meters.
        /// </summary>
        public const double MinimumDimension = 0.000001d;

        /// <summary>
        /// Minimum accepted polygon area in square meters.
        /// </summary>
        public const double MinimumArea = 0.00000001d;

        /// <summary>
        /// Distance tolerance used for duplicate and intersection tests.
        /// </summary>
        public const double Position = 0.000001d;

        /// <summary>
        /// Allowed deviation from a local-Y-aligned plane normal in Phase 5.
        /// </summary>
        public const double PlaneNormal = 0.000001d;

        /// <summary>
        /// Absolute submerged-volume solver tolerance in cubic meters.
        /// </summary>
        public const double SolverAbsoluteVolume = 0.000001d;

        /// <summary>
        /// Capacity-relative submerged-volume solver tolerance.
        /// </summary>
        public const double SolverRelativeVolume = 0.000001d;

        /// <summary>
        /// Surface-plane position tolerance in meters.
        /// </summary>
        public const double SolverPlanePosition = 0.000001d;

        /// <summary>
        /// Maximum bounded binary-search iterations.
        /// </summary>
        public const int SolverMaximumIterations = 64;

        /// <summary>
        /// Gravity magnitudes below this value do not define a new surface direction.
        /// </summary>
        public const double MinimumGravityMagnitude = 0.00001d;
    }
}
