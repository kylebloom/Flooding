using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Advances registered flooding components at a fixed simulation rate and
    /// publishes their committed states in a deterministic phase order.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class FloodSimulationManager : MonoBehaviour
    {
        private const float MinimumTicksPerSecond = 0.1f;

        [Header("Scheduling")]

        [SerializeField]
        [Tooltip("Number of flooding simulation ticks executed per game second.")]
        [Min(MinimumTicksPerSecond)]
        private float ticksPerSecond = 10f;

        [SerializeField]
        [Tooltip("Maximum simulation ticks processed in one frame before excess accumulated time is discarded.")]
        [Min(1)]
        private int maximumTicksPerFrame = 4;

        [SerializeField]
        [Tooltip("Whether this manager advances automatically using scaled game time.")]
        private bool simulateAutomatically = true;

        [Header("Gravity")]

        [SerializeField]
        [Tooltip("Uses global Physics.gravity or a manager-specific world-space gravity vector.")]
        private FloodGravityMode gravityMode;

        [SerializeField]
        [Tooltip("World-space gravity in meters per second squared when Gravity Mode is Custom.")]
        private Vector3 customGravity = new(0f, -9.81f, 0f);

        private readonly List<FloodVolume> volumes = new();
        private readonly List<FloodRegion> regions = new();
        private readonly List<ExternalFluidBoundary> externalBoundaries = new();
        private readonly List<FloodSource> sources = new();
        private readonly List<FloodSink> sinks = new();
        private readonly List<FloodConnection> connections = new();
        private readonly List<FloodConnectionEvaluation> connectionEvaluations = new();
        private readonly List<double> sourceLimitedTransferVolumes = new();
        private readonly List<ConfiguredFlowRequest> configuredInflowRequests = new();
        private readonly List<ConfiguredFlowRequest> configuredOutflowRequests = new();

        private readonly Dictionary<FluidBoundaryId, FluidBoundarySnapshot> boundarySnapshots = new();
        private readonly Dictionary<FloodVolume, FloodState> volumeSnapshots = new();
        private readonly Dictionary<FloodVolume, double> requestedConfiguredInflows = new();
        private readonly Dictionary<FloodVolume, double> requestedOutflows = new();
        private readonly Dictionary<FloodVolume, double> requestedInflows = new();
        private readonly Dictionary<FloodVolume, double> destinationScales = new();
        private readonly Dictionary<FloodVolume, double> volumeDeltas = new();

        private ReadOnlyCollection<FloodVolume> registeredVolumesView;

        private double accumulatedTime;

        /// <summary>
        /// Gets or sets the number of simulation ticks per game second.
        /// </summary>
        public float TicksPerSecond
        {
            get => ticksPerSecond;
            set
            {
                EnsureFinite(value, nameof(value));
                ticksPerSecond = Mathf.Max(MinimumTicksPerSecond, value);
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of ticks processed in one frame.
        /// </summary>
        public int MaximumTicksPerFrame
        {
            get => maximumTicksPerFrame;
            set => maximumTicksPerFrame = Mathf.Max(1, value);
        }

        /// <summary>
        /// Gets or sets whether scaled game time advances this manager.
        /// </summary>
        public bool SimulateAutomatically
        {
            get => simulateAutomatically;
            set => simulateAutomatically = value;
        }

        /// <summary>
        /// Gets or sets the gravity source used by managed flood volumes.
        /// </summary>
        public FloodGravityMode GravityMode
        {
            get => gravityMode;
            set => gravityMode = value;
        }

        /// <summary>
        /// Gets or sets manager-specific world-space gravity.
        /// </summary>
        public Vector3 CustomGravity
        {
            get => customGravity;
            set
            {
                EnsureFinite(value.x, nameof(value));
                EnsureFinite(value.y, nameof(value));
                EnsureFinite(value.z, nameof(value));
                customGravity = value;
            }
        }

        /// <summary>
        /// Gets the currently selected world-space gravity vector.
        /// </summary>
        public Vector3 ActiveGravity =>
            gravityMode == FloodGravityMode.Custom
                ? customGravity
                : Physics.gravity;

        /// <summary>
        /// Gets the fixed duration of one simulation tick in seconds.
        /// </summary>
        public double TickInterval => 1d / ticksPerSecond;

        /// <summary>
        /// Gets the number of whole ticks discarded by the frame catch-up
        /// limit since this manager was enabled.
        /// </summary>
        public long DiscardedTickCount { get; private set; }

        /// <summary>
        /// Gets volume accounting from the most recently completed tick.
        /// </summary>
        public FloodTickMetrics LastTickMetrics { get; private set; }

        /// <summary>
        /// Gets the flood volumes currently registered with this manager, in
        /// registration order.
        /// </summary>
        /// <remarks>
        /// The returned collection is a live read-only view. Consumers must not
        /// register or unregister volumes through it; ownership of membership
        /// remains with this manager. Destroyed entries may appear as null until
        /// the next simulation tick removes them.
        /// </remarks>
        public IReadOnlyList<FloodVolume> RegisteredVolumes =>
            registeredVolumesView ??= volumes.AsReadOnly();

        /// <summary>
        /// Raised after one tick has committed all changes and published all
        /// registered volume states.
        /// </summary>
        public event Action<double> TickCompleted;

        private void OnEnable()
        {
            accumulatedTime = 0d;
            DiscardedTickCount = 0;
            RegisterHierarchyComponents();
        }

        private void Update()
        {
            if (simulateAutomatically)
                Advance(Time.deltaTime);
        }

        private void OnValidate()
        {
            if (float.IsNaN(ticksPerSecond) || float.IsInfinity(ticksPerSecond))
                ticksPerSecond = 10f;
            else
                ticksPerSecond = Mathf.Max(MinimumTicksPerSecond, ticksPerSecond);

            maximumTicksPerFrame = Mathf.Max(1, maximumTicksPerFrame);

            if (!IsFinite(customGravity))
                customGravity = new Vector3(0f, -9.81f, 0f);
        }

        /// <summary>
        /// Adds elapsed scaled game time and executes any due fixed ticks.
        /// </summary>
        /// <param name="deltaTime">Elapsed time in seconds.</param>
        public void Advance(double deltaTime)
        {
            EnsureFiniteNonNegative(deltaTime, nameof(deltaTime));

            if (deltaTime <= 0d)
                return;

            accumulatedTime += deltaTime;

            var tickInterval = TickInterval;

            if (double.IsInfinity(accumulatedTime))
            {
                accumulatedTime = tickInterval * maximumTicksPerFrame;
                DiscardedTickCount = long.MaxValue;
            }

            var processedTicks = 0;

            while (
                accumulatedTime >= tickInterval
                && processedTicks < maximumTicksPerFrame)
            {
                SimulateTick(tickInterval);
                accumulatedTime -= tickInterval;
                processedTicks++;
            }

            if (accumulatedTime < tickInterval)
                return;

            var discardedTicks = Math.Floor(accumulatedTime / tickInterval);
            AddDiscardedTicks(discardedTicks);
            accumulatedTime %= tickInterval;
        }

        /// <summary>
        /// Executes exactly one flooding tick using the supplied duration.
        /// </summary>
        /// <param name="deltaTime">Tick duration in seconds.</param>
        public void SimulateTick(double deltaTime)
        {
            EnsureFiniteNonNegative(deltaTime, nameof(deltaTime));

            if (deltaTime <= 0d)
                return;

            RemoveMissingRegistrations();
            CaptureBoundarySnapshots();
            CalculateRequestedConfiguredInflows(deltaTime);
            CalculateRequestedConfiguredOutflows(deltaTime);
            EvaluateConnections(deltaTime);
            ReconcileTransfers(deltaTime);
            CommitVolumeDeltas();
            PublishVolumeStates();
            LastTickMetrics = BuildTickMetrics();

            TickCompleted?.Invoke(deltaTime);
        }

        internal void Register(FloodVolume volume)
        {
            if (volume != null && !volumes.Contains(volume))
                volumes.Add(volume);
        }

        internal void Unregister(FloodVolume volume)
        {
            if (volume != null)
                volumes.Remove(volume);
        }

        internal void Register(FloodRegion region)
        {
            if (region != null && !regions.Contains(region))
                regions.Add(region);
        }

        internal void Unregister(FloodRegion region)
        {
            if (region != null)
                regions.Remove(region);
        }

        internal void Register(ExternalFluidBoundary boundary)
        {
            if (boundary != null && !externalBoundaries.Contains(boundary))
                externalBoundaries.Add(boundary);
        }

        internal void Unregister(ExternalFluidBoundary boundary)
        {
            if (boundary != null)
                externalBoundaries.Remove(boundary);
        }

        internal void Register(FloodSource source)
        {
            if (source != null && !sources.Contains(source))
                sources.Add(source);
        }

        internal void Unregister(FloodSource source)
        {
            if (source != null)
                sources.Remove(source);
        }

        internal void Register(FloodSink sink)
        {
            if (sink != null && !sinks.Contains(sink))
                sinks.Add(sink);
        }

        internal void Unregister(FloodSink sink)
        {
            if (sink != null)
                sinks.Remove(sink);
        }

        internal void Register(FloodConnection connection)
        {
            if (connection != null && !connections.Contains(connection))
                connections.Add(connection);
        }

        internal void Unregister(FloodConnection connection)
        {
            if (connection != null)
                connections.Remove(connection);
        }

        private void RegisterHierarchyComponents()
        {
            var childRegions = GetComponentsInChildren<FloodRegion>(true);

            foreach (var region in childRegions)
                region.UseManagerIfUnset(this);

            var childVolumes = GetComponentsInChildren<FloodVolume>(true);

            foreach (var volume in childVolumes)
                volume.UseManagerIfUnset(this);

            var childExternals =
                GetComponentsInChildren<ExternalFluidBoundary>(true);

            foreach (var boundary in childExternals)
                boundary.UseManagerIfUnset(this);

            var childSources = GetComponentsInChildren<FloodSource>(true);

            foreach (var source in childSources)
                source.UseManagerIfUnset(this);

            var childSinks = GetComponentsInChildren<FloodSink>(true);

            foreach (var sink in childSinks)
                sink.UseManagerIfUnset(this);

            var childConnections = GetComponentsInChildren<FloodConnection>(true);

            foreach (var connection in childConnections)
                connection.UseManagerIfUnset(this);
        }

        private void RemoveMissingRegistrations()
        {
            volumes.RemoveAll(volume => volume == null);
            regions.RemoveAll(region => region == null);
            externalBoundaries.RemoveAll(boundary => boundary == null);
            sources.RemoveAll(source => source == null);
            sinks.RemoveAll(sink => sink == null);
            connections.RemoveAll(connection => connection == null);
        }

        private void CaptureBoundarySnapshots()
        {
            boundarySnapshots.Clear();
            volumeSnapshots.Clear();

            foreach (var region in regions)
                region.BeginTick();

            foreach (var volume in volumes)
            {
                if (
                    !volume.isActiveAndEnabled
                    || volume.SimulationManager != this
                    || !IsWaterCommitParticipant(volume))
                {
                    continue;
                }

                var snapshot = volume.CaptureBoundarySnapshot();
                boundarySnapshots[snapshot.BoundaryId] = snapshot;
                volumeSnapshots[volume] = volume.CurrentState;
            }

            foreach (var boundary in externalBoundaries)
            {
                if (
                    boundary.IsBoundaryEnabled
                    && boundary.SimulationManager == this)
                {
                    var snapshot = boundary.CaptureBoundarySnapshot();
                    boundarySnapshots[snapshot.BoundaryId] = snapshot;
                }
            }
        }

        private void CalculateRequestedConfiguredInflows(double deltaTime)
        {
            requestedConfiguredInflows.Clear();
            configuredInflowRequests.Clear();

            foreach (var source in sources)
                source.ResetTickState();

            foreach (var source in sources)
            {
                if (
                    !source.TryGetRequestedInflow(
                        this,
                        deltaTime,
                        out var target,
                        out var requestedVolume))
                {
                    continue;
                }

                target = EffectiveFluidBoundary.ResolveCommitVolume(target);
                if (target == null || !volumeSnapshots.ContainsKey(target))
                    continue;

                configuredInflowRequests.Add(
                    new ConfiguredFlowRequest(source, null, target, requestedVolume));
                AddAmount(
                    requestedConfiguredInflows,
                    target,
                    requestedVolume);
            }
        }

        private void CalculateRequestedConfiguredOutflows(double deltaTime)
        {
            configuredOutflowRequests.Clear();

            foreach (var sink in sinks)
                sink.ResetTickState();

            foreach (var sink in sinks)
            {
                if (
                    !sink.TryGetRequestedOutflow(
                        this,
                        deltaTime,
                        out var target,
                        out var requestedVolume))
                {
                    continue;
                }

                target = EffectiveFluidBoundary.ResolveCommitVolume(target);
                if (target == null || !volumeSnapshots.ContainsKey(target))
                    continue;

                configuredOutflowRequests.Add(
                    new ConfiguredFlowRequest(null, sink, target, requestedVolume));
            }
        }

        private void EvaluateConnections(double deltaTime)
        {
            connectionEvaluations.Clear();
            var gravityMagnitude = ActiveGravity.magnitude;

            foreach (var connection in connections)
            {
                if (
                    connection.TryEvaluate(
                        this,
                        boundarySnapshots,
                        deltaTime,
                        gravityMagnitude,
                        out var evaluation))
                {
                    connectionEvaluations.Add(evaluation);
                }
            }
        }

        private void ReconcileTransfers(double deltaTime)
        {
            requestedOutflows.Clear();
            requestedInflows.Clear();
            destinationScales.Clear();
            volumeDeltas.Clear();
            sourceLimitedTransferVolumes.Clear();

            CalculateRequestedOutflows();
            CalculateSourceLimitedTransfers();
            CalculateDestinationScales();
            ApplyConfiguredInflows(deltaTime);
            ApplyConfiguredOutflows(deltaTime);
            ApplyConnectionTransfers(deltaTime);
        }

        private void CalculateRequestedOutflows()
        {
            foreach (var evaluation in connectionEvaluations)
            {
                if (!evaluation.HasTransfer || !evaluation.SourceHasFiniteSupply)
                    continue;

                AddAmount(
                    requestedOutflows,
                    evaluation.FiniteSource,
                    evaluation.RequestedVolume);
            }

            foreach (var request in configuredOutflowRequests)
            {
                AddAmount(
                    requestedOutflows,
                    request.Target,
                    request.RequestedVolume);
            }
        }

        private void CalculateSourceLimitedTransfers()
        {
            foreach (var evaluation in connectionEvaluations)
            {
                var sourceLimitedVolume = 0d;

                if (evaluation.HasTransfer)
                {
                    if (!evaluation.SourceHasFiniteSupply)
                    {
                        sourceLimitedVolume = evaluation.RequestedVolume;
                    }
                    else if (
                        evaluation.FiniteSource != null
                        && volumeSnapshots.TryGetValue(
                            evaluation.FiniteSource,
                            out var sourceSnapshot)
                        && requestedOutflows.TryGetValue(
                            evaluation.FiniteSource,
                            out var totalRequestedOutflow)
                        && totalRequestedOutflow > 0d)
                    {
                        var sourceScale = Math.Min(
                            1d,
                            sourceSnapshot.Volume / totalRequestedOutflow);

                        sourceLimitedVolume =
                            evaluation.RequestedVolume * sourceScale;
                    }

                    if (
                        evaluation.DestinationHasFiniteCapacity
                        && evaluation.FiniteDestination != null
                        && sourceLimitedVolume > 0d)
                    {
                        AddAmount(
                            requestedInflows,
                            evaluation.FiniteDestination,
                            sourceLimitedVolume);
                    }
                }

                sourceLimitedTransferVolumes.Add(sourceLimitedVolume);
            }

            foreach (var configuredInflow in requestedConfiguredInflows)
            {
                AddAmount(
                    requestedInflows,
                    configuredInflow.Key,
                    configuredInflow.Value);
            }
        }

        private void CalculateDestinationScales()
        {
            foreach (var requestedInflow in requestedInflows)
            {
                if (
                    !volumeSnapshots.TryGetValue(
                        requestedInflow.Key,
                        out var destinationSnapshot)
                    || requestedInflow.Value <= 0d)
                {
                    destinationScales[requestedInflow.Key] = 0d;
                    continue;
                }

                var availableCapacity = Math.Max(
                    0d,
                    destinationSnapshot.Capacity
                    - destinationSnapshot.Volume);

                destinationScales[requestedInflow.Key] = Math.Min(
                    1d,
                    availableCapacity / requestedInflow.Value);
            }
        }

        private void ApplyConfiguredInflows(double deltaTime)
        {
            foreach (var request in configuredInflowRequests)
            {
                var acceptedVolume =
                    request.RequestedVolume
                    * GetDestinationScale(request.Target);

                AddAmount(volumeDeltas, request.Target, acceptedVolume);
                request.Source?.ApplyTickResult(acceptedVolume / deltaTime);
            }
        }

        private void ApplyConfiguredOutflows(double deltaTime)
        {
            foreach (var request in configuredOutflowRequests)
            {
                var acceptedVolume = 0d;

                if (
                    volumeSnapshots.TryGetValue(request.Target, out var snapshot)
                    && requestedOutflows.TryGetValue(
                        request.Target,
                        out var totalRequestedOutflow)
                    && totalRequestedOutflow > 0d)
                {
                    var sourceScale = Math.Min(
                        1d,
                        snapshot.Volume / totalRequestedOutflow);
                    acceptedVolume = request.RequestedVolume * sourceScale;
                }

                AddAmount(volumeDeltas, request.Target, -acceptedVolume);
                request.Sink?.ApplyTickResult(acceptedVolume / deltaTime);
            }
        }

        private void ApplyConnectionTransfers(double deltaTime)
        {
            for (var index = 0; index < connectionEvaluations.Count; index++)
            {
                var evaluation = connectionEvaluations[index];
                var destinationScale =
                    evaluation.DestinationHasFiniteCapacity
                        ? GetDestinationScale(evaluation.FiniteDestination)
                        : 1d;
                var acceptedVolume =
                    sourceLimitedTransferVolumes[index] * destinationScale;

                var direction =
                    evaluation.Flow.SignedFlowRate > 0d
                        ? 1d
                        : evaluation.Flow.SignedFlowRate < 0d
                            ? -1d
                            : 0d;

                evaluation.Connection.ApplyTickResult(
                    evaluation.Flow,
                    direction * acceptedVolume / deltaTime);

                if (acceptedVolume <= 0d)
                    continue;

                if (evaluation.FiniteSource != null)
                {
                    AddAmount(
                        volumeDeltas,
                        evaluation.FiniteSource,
                        -acceptedVolume);
                }

                if (evaluation.FiniteDestination != null)
                {
                    AddAmount(
                        volumeDeltas,
                        evaluation.FiniteDestination,
                        acceptedVolume);
                }
            }
        }

        private void CommitVolumeDeltas()
        {
            foreach (var volume in volumes)
            {
                if (!IsWaterCommitParticipant(volume))
                    continue;

                if (volumeDeltas.TryGetValue(volume, out var delta))
                    volume.ApplyManagedVolumeDelta(delta);
            }
        }

        private void PublishVolumeStates()
        {
            foreach (var volume in volumes)
            {
                if (volumeSnapshots.ContainsKey(volume))
                    volume.PublishManagedState();
            }
        }

        private static bool IsWaterCommitParticipant(FloodVolume volume)
        {
            var region = volume.OwningRegion;
            return region == null || region.IsCommitParticipant(volume);
        }

        private FloodTickMetrics BuildTickMetrics()
        {
            var finiteVolumeBefore = 0d;
            var finiteVolumeAfter = 0d;
            var configuredSourceVolume = 0d;
            var configuredSinkVolume = 0d;
            var internalTransferVolume = 0d;
            var externalInflowVolume = 0d;
            var externalOutflowVolume = 0d;

            foreach (var pair in volumeSnapshots)
            {
                finiteVolumeBefore += pair.Value.Volume;
                finiteVolumeAfter += pair.Key.CurrentState.Volume;
            }

            foreach (var request in configuredInflowRequests)
            {
                configuredSourceVolume +=
                    request.RequestedVolume
                    * GetDestinationScale(request.Target);
            }

            foreach (var request in configuredOutflowRequests)
            {
                if (
                    !volumeSnapshots.TryGetValue(
                        request.Target,
                        out var snapshot)
                    || !requestedOutflows.TryGetValue(
                        request.Target,
                        out var totalRequestedOutflow)
                    || totalRequestedOutflow <= 0d)
                {
                    continue;
                }

                var sourceScale = Math.Min(
                    1d,
                    snapshot.Volume / totalRequestedOutflow);
                configuredSinkVolume += request.RequestedVolume * sourceScale;
            }

            for (var index = 0; index < connectionEvaluations.Count; index++)
            {
                var evaluation = connectionEvaluations[index];
                var destinationScale =
                    evaluation.DestinationHasFiniteCapacity
                        ? GetDestinationScale(evaluation.FiniteDestination)
                        : 1d;
                var acceptedVolume =
                    sourceLimitedTransferVolumes[index] * destinationScale;

                if (acceptedVolume <= 0d)
                    continue;

                var sourceFinite = evaluation.SourceHasFiniteSupply;
                var destinationFinite = evaluation.DestinationHasFiniteCapacity;

                if (sourceFinite && destinationFinite)
                    internalTransferVolume += acceptedVolume;
                else if (!sourceFinite && destinationFinite)
                    externalInflowVolume += acceptedVolume;
                else if (sourceFinite && !destinationFinite)
                    externalOutflowVolume += acceptedVolume;
            }

            return new FloodTickMetrics(
                internalTransferVolume,
                externalInflowVolume,
                externalOutflowVolume,
                configuredSourceVolume,
                configuredSinkVolume,
                finiteVolumeBefore,
                finiteVolumeAfter);
        }

        private double GetDestinationScale(FloodVolume destination)
        {
            return
                destination != null
                && destinationScales.TryGetValue(
                    destination,
                    out var scale)
                    ? scale
                    : 0d;
        }

        private void AddDiscardedTicks(double discardedTicks)
        {
            if (
                discardedTicks >= long.MaxValue
                || DiscardedTickCount >= long.MaxValue - discardedTicks)
            {
                DiscardedTickCount = long.MaxValue;
                return;
            }

            DiscardedTickCount += (long)discardedTicks;
        }

        private static void AddAmount(
            IDictionary<FloodVolume, double> amounts,
            FloodVolume volume,
            double amount)
        {
            if (volume == null || amount == 0d)
                return;

            amounts.TryGetValue(volume, out var existingAmount);
            var combinedAmount = existingAmount + amount;

            amounts[volume] =
                double.IsPositiveInfinity(combinedAmount)
                    ? double.MaxValue
                    : double.IsNegativeInfinity(combinedAmount)
                        ? -double.MaxValue
                        : combinedAmount;
        }

        private static void EnsureFiniteNonNegative(
            double value,
            string parameterName)
        {
            if (
                double.IsNaN(value)
                || double.IsInfinity(value)
                || value < 0d)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }
        }

        private static void EnsureFinite(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentOutOfRangeException(parameterName);
        }

        private static bool IsFinite(Vector3 value)
        {
            return !float.IsNaN(value.x)
                && !float.IsNaN(value.y)
                && !float.IsNaN(value.z)
                && !float.IsInfinity(value.x)
                && !float.IsInfinity(value.y)
                && !float.IsInfinity(value.z);
        }

        private readonly struct ConfiguredFlowRequest
        {
            public ConfiguredFlowRequest(
                FloodSource source,
                FloodSink sink,
                FloodVolume target,
                double requestedVolume)
            {
                Source = source;
                Sink = sink;
                Target = target;
                RequestedVolume = requestedVolume;
            }

            public FloodSource Source { get; }

            public FloodSink Sink { get; }

            public FloodVolume Target { get; }

            public double RequestedVolume { get; }
        }
    }
}
