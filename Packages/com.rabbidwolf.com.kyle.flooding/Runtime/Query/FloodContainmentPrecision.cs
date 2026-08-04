namespace Kyle.Flooding
{
    /// <summary>
    /// Describes how precisely <see cref="IFloodVolumeGeometry.ContainsLocalPoint"/>
    /// matches the authored floodable space.
    /// </summary>
    public enum FloodContainmentPrecision
    {
        /// <summary>
        /// Containment matches the analytic authored shape exactly
        /// (rectangular prism or extruded polygon).
        /// </summary>
        Exact = 0,

        /// <summary>
        /// Containment uses Editor-baked occupancy cells and is therefore
        /// resolution-dependent.
        /// </summary>
        BakeApproximation = 1,
    }
}
