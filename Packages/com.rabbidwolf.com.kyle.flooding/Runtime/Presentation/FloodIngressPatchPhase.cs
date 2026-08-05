namespace Kyle.Flooding
{
    /// <summary>
    /// Lifecycle phase for a local ingress presentation patch.
    /// </summary>
    public enum FloodIngressPatchPhase : byte
    {
        /// <summary>
        /// Slot is unused.
        /// </summary>
        Inactive = 0,

        /// <summary>
        /// Provider is actively delivering inflow; the local patch expands.
        /// </summary>
        Growing = 1,

        /// <summary>
        /// Provider stopped; the patch remains visible before bulk handoff.
        /// </summary>
        Settling = 2,

        /// <summary>
        /// Local presentation fades toward the authoritative bulk surface.
        /// </summary>
        Converging = 3,
    }
}
