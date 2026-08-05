namespace Kyle.Flooding
{
    /// <summary>
    /// Volume accounting for one completed simulation tick.
    /// </summary>
    public readonly struct FloodTickMetrics
    {
        internal FloodTickMetrics(
            double internalTransferVolume,
            double externalInflowVolume,
            double externalOutflowVolume,
            double configuredSourceVolume,
            double configuredSinkVolume,
            double finiteVolumeBefore,
            double finiteVolumeAfter)
        {
            InternalTransferVolume = internalTransferVolume;
            ExternalInflowVolume = externalInflowVolume;
            ExternalOutflowVolume = externalOutflowVolume;
            ConfiguredSourceVolume = configuredSourceVolume;
            ConfiguredSinkVolume = configuredSinkVolume;
            FiniteVolumeBefore = finiteVolumeBefore;
            FiniteVolumeAfter = finiteVolumeAfter;
        }

        /// <summary>
        /// Gets total volume transferred between two finite volumes, in cubic
        /// meters.
        /// </summary>
        public double InternalTransferVolume { get; }

        /// <summary>
        /// Gets volume that entered finite volumes from external boundaries, in
        /// cubic meters.
        /// </summary>
        public double ExternalInflowVolume { get; }

        /// <summary>
        /// Gets volume that left finite volumes to external boundaries, in
        /// cubic meters.
        /// </summary>
        public double ExternalOutflowVolume { get; }

        /// <summary>
        /// Gets volume actually injected by configured <see cref="FloodSource"/>
        /// components after destination-capacity scaling, in cubic meters.
        /// </summary>
        public double ConfiguredSourceVolume { get; }

        /// <summary>
        /// Gets volume actually removed by configured <see cref="FloodSink"/>
        /// components after supply scaling, in cubic meters.
        /// </summary>
        public double ConfiguredSinkVolume { get; }

        /// <summary>
        /// Gets total finite-compartment volume at tick start, in cubic meters.
        /// </summary>
        public double FiniteVolumeBefore { get; }

        /// <summary>
        /// Gets total finite-compartment volume after commit, in cubic meters.
        /// </summary>
        public double FiniteVolumeAfter { get; }

        /// <summary>
        /// Gets the closed-system conservation residual for finite volumes.
        /// </summary>
        /// <remarks>
        /// Expected identity using applied amounts:
        /// after = before + external inflow - external outflow + configured
        /// sources - configured sinks.
        /// </remarks>
        public double ConservationError =>
            FiniteVolumeAfter
            - (
                FiniteVolumeBefore
                + ExternalInflowVolume
                - ExternalOutflowVolume
                + ConfiguredSourceVolume
                - ConfiguredSinkVolume);
    }
}
