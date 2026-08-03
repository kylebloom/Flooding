namespace Kyle.Flooding
{
    /// <summary>
    /// Selects the authored geometry representation used by a FloodVolume.
    /// </summary>
    public enum FloodGeometryMode
    {
        /// <summary>
        /// Centered rectangle configured by width and length.
        /// </summary>
        RectangularPrism = 0,

        /// <summary>
        /// One simple local-XZ polygon extruded along local Y.
        /// </summary>
        ExtrudedPolygon = 1,

        /// <summary>
        /// Immutable Editor-baked data for complex closed meshes.
        /// </summary>
        BakedData = 2,
    }
}
