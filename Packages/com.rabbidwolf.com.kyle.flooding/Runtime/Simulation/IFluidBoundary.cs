namespace Kyle.Flooding
{
    /// <summary>
    /// Scene endpoint that can participate in pressure-driven flood connections.
    /// </summary>
    public interface IFluidBoundary
    {
        /// <summary>
        /// Gets the stable identity used for snapshot lookup and validation.
        /// </summary>
        FluidBoundaryId BoundaryId { get; }

        /// <summary>
        /// Gets the manager that owns this boundary.
        /// </summary>
        FloodSimulationManager SimulationManager { get; }

        /// <summary>
        /// Gets whether this boundary currently participates in simulation.
        /// </summary>
        bool IsBoundaryEnabled { get; }

        /// <summary>
        /// Captures immutable tick-start facts for this boundary.
        /// </summary>
        FluidBoundarySnapshot CaptureBoundarySnapshot();
    }
}
