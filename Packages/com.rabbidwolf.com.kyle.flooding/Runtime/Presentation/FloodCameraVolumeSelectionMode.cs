namespace Kyle.Flooding
{
    /// <summary>
    /// Selects how <see cref="FloodCameraTracker"/> chooses its active
    /// <see cref="FloodVolume"/>.
    /// </summary>
    public enum FloodCameraVolumeSelectionMode
    {
        /// <summary>
        /// Always track the explicitly assigned volume.
        /// </summary>
        Explicit = 0,

        /// <summary>
        /// Choose among volumes registered with a
        /// <see cref="FloodSimulationManager"/> using sticky containment
        /// selection.
        /// </summary>
        AutoDiscoverRegistered = 1,
    }
}
