using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Independently simulated equilibrium water body composed from one or more
    /// explicit member <see cref="FloodVolume"/> geometries.
    /// </summary>
    /// <remarks>
    /// Membership is authoring truth. Geometry validates spatial continuity but
    /// never invents membership. <see cref="InitialVolume"/> is authoritative
    /// for the region; member volume initial-state fields are inactive while
    /// bound. Topology is static for v1 — opening a door must use a
    /// <see cref="FloodConnection"/> between separate regions.
    /// </remarks>
    [DefaultExecutionOrder(-500)]
    [DisallowMultipleComponent]
    public sealed class FloodRegion : MonoBehaviour, IFluidBoundary, IMassContributor
    {
        private const float MinimumDimension = 0.01f;
        private const float MinimumDensity = 0.01f;
        private const float MinimumCellResolution = 0.01f;

        [Header("Simulation")]

        [SerializeField]
        [Tooltip("Manager that advances and publishes this region. If unassigned, the nearest parent manager is used.")]
        private FloodSimulationManager simulationManager;

        [Header("Members")]

        [SerializeField]
        [Tooltip("Explicit FloodVolume members that compose this region's floodable geometry. Membership is authoring truth; overlap or face-sharing is validated, not discovered.")]
        private List<FloodVolume> members = new();

        [Header("Baked Region Geometry")]

        [SerializeField]
        [Tooltip("Optional Editor-baked Flood Region Data asset for N-member or mixed-geometry unions. When usable, CompositeFloodGeometry prefers RegionOccupancyUnionStrategy over the two-box analytic path. Leave empty for one-member regions or eligible two rectangular members.")]
        private FloodRegionData bakedRegionData;

        [SerializeField]
        [Tooltip("Requested maximum region-local cell edge length in meters for Bake Region. Smaller values improve boundary fidelity but increase bake time, asset size, and runtime query cost.")]
        [Min(MinimumCellResolution)]
        private float cellResolution = 0.25f;

        [SerializeField]
        [Tooltip("Maximum number of region grid cells the Editor baker may inspect. The bake stops instead of creating an unexpectedly large asset.")]
        [Min(1)]
        private int maximumGridCells = 1000000;

        [SerializeField]
        [Tooltip("Draw baked region occupancy cells and bounds in the Scene view while this FloodRegion is selected.")]
        private bool visualizeBake = true;

        [Header("Fluid")]

        [SerializeField]
        [Tooltip("Water density in kilograms per cubic meter. Fresh water is approximately 1000 kg/m³. Must match all member densities.")]
        [Min(MinimumDensity)]
        private float waterDensity = 1000f;

        [Header("Initial State")]

        [SerializeField]
        [Tooltip("Authoritative water volume present when Play Mode begins, in cubic meters. Member FloodVolume Initial Volume fields are ignored while bound to this region. Values above capacity are clamped.")]
        [Min(0f)]
        private float initialVolume;

        private IFloodVolumeGeometry geometry;
        private FloodSimulation simulation;
        private FloodState previousState;
        private bool hasPreviousState;
        private FloodSurfaceSolution cachedSurfaceSolution;
        private Vector3 cachedSurfaceNormal;
        private double cachedSurfaceVolume;
        private bool hasCachedSurfaceSolution;
        private Vector3 lastValidLocalSurfaceNormal = Vector3.up;
        private bool hasLastValidSurfaceNormal;
        private string validationMessage;
        private readonly List<FloodVolume> boundMembers = new();

        /// <summary>
        /// Gets or sets the manager that advances this region.
        /// </summary>
        public FloodSimulationManager SimulationManager
        {
            get => simulationManager;
            set => SetSimulationManager(value);
        }

        /// <summary>
        /// Gets the authored member list.
        /// </summary>
        public IReadOnlyList<FloodVolume> Members => members;

        /// <summary>
        /// Gets members successfully bound at runtime.
        /// </summary>
        public IReadOnlyList<FloodVolume> BoundMembers => boundMembers;

        /// <summary>
        /// Gets the active region geometry (member geometry or composite).
        /// </summary>
        public IFloodVolumeGeometry Geometry => geometry;

        /// <summary>
        /// Gets the optional Editor-baked region occupancy asset.
        /// </summary>
        public FloodRegionData BakedRegionData => bakedRegionData;

        /// <summary>
        /// Gets requested maximum region-local cell edge length in meters.
        /// </summary>
        public float CellResolution => cellResolution;

        /// <summary>
        /// Gets the Editor bake grid-cell safety limit.
        /// </summary>
        public int MaximumGridCells => maximumGridCells;

        /// <summary>
        /// Gets whether selected-object bake visualization is enabled.
        /// </summary>
        public bool VisualizeBake => visualizeBake;

        /// <summary>
        /// Gets the authoritative authored initial water volume in cubic meters.
        /// </summary>
        public float InitialVolume => initialVolume;

        /// <summary>
        /// Gets the configured water density in kilograms per cubic meter.
        /// </summary>
        public float WaterDensity => waterDensity;

        /// <inheritdoc />
        public FluidBoundaryId BoundaryId => FluidBoundaryId.FromObject(this);

        /// <inheritdoc />
        public bool IsBoundaryEnabled =>
            isActiveAndEnabled && simulation != null && geometry != null;

        /// <summary>
        /// Gets the latest region validation message, if any.
        /// </summary>
        public string ValidationMessage => validationMessage;

        /// <summary>
        /// Gets the current water volume in cubic meters.
        /// </summary>
        public float CurrentVolume =>
            simulation == null
                ? 0f
                : (float)simulation.CurrentVolume;

        /// <summary>
        /// Gets the equivalent level-fill height in meters.
        /// </summary>
        public float CurrentHeight =>
            simulation == null
                ? 0f
                : (float)simulation.CurrentHeight;

        /// <summary>
        /// Gets the region capacity in cubic meters.
        /// </summary>
        public float MaximumVolume =>
            simulation == null
                ? (float)(geometry?.Capacity ?? 0d)
                : (float)simulation.MaximumVolume;

        /// <summary>
        /// Gets the normalized fill percentage from zero to one.
        /// </summary>
        public float FillPercentage =>
            simulation == null
                ? 0f
                : (float)simulation.FillPercentage;

        /// <summary>
        /// Gets the current water mass in kilograms.
        /// </summary>
        public double WaterMass =>
            (simulation?.CurrentVolume ?? 0d) * waterDensity;

        /// <summary>
        /// Gets the current world-space water surface plane.
        /// </summary>
        public Plane SurfacePlane =>
            geometry == null
                ? new Plane(transform.up, transform.position)
                : FloodPlaneUtility.LocalToWorld(
                    transform,
                    ResolveSurfaceSolution().LocalSurfacePlane);

        /// <summary>
        /// Gets the solved water surface plane in region-local space.
        /// </summary>
        public Plane LocalSurfacePlane =>
            geometry == null
                ? new Plane(Vector3.up, Vector3.zero)
                : ResolveSurfaceSolution().LocalSurfacePlane;

        /// <summary>
        /// Gets the current world-space center of mass of the water.
        /// </summary>
        public Vector3 WaterCenterOfMassWorld =>
            geometry == null
                ? transform.position
                : transform.TransformPoint(
                    ResolveSurfaceSolution().Submersion.Centroid);

        double IMassContributor.Mass => WaterMass;

        Vector3 IMassContributor.CenterOfMassWorld =>
            WaterCenterOfMassWorld;

        /// <summary>
        /// Gets an immutable snapshot of the current public state.
        /// </summary>
        public FloodState CurrentState => CaptureState();

        /// <summary>
        /// Raised after the manager publishes a flood state change.
        /// </summary>
        public event Action<FloodState> StateChanged;

        /// <summary>
        /// Raised after the current volume changes.
        /// </summary>
        public event Action<double> VolumeChanged;

        /// <summary>
        /// Raised after the equivalent level-fill height changes.
        /// </summary>
        public event Action<float> WaterHeightChanged;

        /// <summary>
        /// Returns whether a world-space point lies inside the composite
        /// floodable union.
        /// </summary>
        public bool ContainsPoint(Vector3 worldPoint)
        {
            return QueryPoint(worldPoint).IsInsideVolume;
        }

        /// <summary>
        /// Returns whether a world-space point is inside the composite union
        /// and below the shared water surface plane.
        /// </summary>
        public bool IsPointSubmerged(Vector3 worldPoint)
        {
            return QueryPoint(worldPoint).IsSubmerged;
        }

        /// <summary>
        /// Queries submersion using composite-union containment and this
        /// region's shared water state.
        /// </summary>
        public FloodQueryResult QueryPoint(Vector3 worldPoint)
        {
            var activeGeometry = geometry;
            var isInsideVolume = activeGeometry != null
                && activeGeometry.ContainsLocalPoint(
                    transform.InverseTransformPoint(worldPoint));

            var surfacePlane = SurfacePlane;
            var surfaceSignedDistance =
                surfacePlane.GetDistanceToPoint(worldPoint);
            var submersionDepth = Mathf.Max(0f, -surfaceSignedDistance);
            var isSubmerged = isInsideVolume && submersionDepth > 0f;

            return new FloodQueryResult(
                isInsideVolume,
                isSubmerged,
                isSubmerged ? submersionDepth : 0f,
                surfacePlane.ClosestPointOnPlane(worldPoint),
                surfacePlane.normal,
                surfaceSignedDistance);
        }

        /// <summary>
        /// Attempts to add water to this region.
        /// </summary>
        public VolumeChangeResult AddWater(float cubicMeters)
        {
            return simulation == null
                ? CreateUnavailableResult(Math.Max(0f, cubicMeters))
                : simulation.AddVolume(cubicMeters);
        }

        /// <summary>
        /// Attempts to remove water from this region.
        /// </summary>
        public VolumeChangeResult RemoveWater(float cubicMeters)
        {
            return simulation == null
                ? CreateUnavailableResult(-Math.Max(0f, cubicMeters))
                : simulation.RemoveVolume(cubicMeters);
        }

        /// <summary>
        /// Configures the region's authoritative initial volume in cubic meters.
        /// </summary>
        public void ConfigureInitialVolume(float cubicMeters)
        {
            if (float.IsNaN(cubicMeters) || float.IsInfinity(cubicMeters))
                throw new ArgumentOutOfRangeException(nameof(cubicMeters));

            initialVolume = Mathf.Max(0f, cubicMeters);

            if (geometry != null)
            {
                initialVolume = Mathf.Clamp(
                    initialVolume,
                    0f,
                    (float)geometry.Capacity);
            }

            if (simulation != null)
                simulation.SetVolume(initialVolume);
            else if (isActiveAndEnabled)
                TryInitializeRegion();
        }

        /// <summary>
        /// Replaces the authored member list. Topology is static after a
        /// successful initialize in v1; call before the region first initializes
        /// or use <see cref="Rebuild"/> for test setup.
        /// </summary>
        public void SetMembers(IReadOnlyList<FloodVolume> newMembers)
        {
            members = newMembers == null
                ? new List<FloodVolume>()
                : new List<FloodVolume>(newMembers);

            if (isActiveAndEnabled)
                Rebuild();
        }

        /// <summary>
        /// Rebuilds region geometry, simulation, and member bindings from the
        /// current authored member list and <see cref="InitialVolume"/>.
        /// </summary>
        public bool Rebuild()
        {
            UnbindMembers();
            simulation = null;
            geometry = null;
            hasCachedSurfaceSolution = false;
            hasPreviousState = false;
            return TryInitializeRegion();
        }

        private void Awake()
        {
            TryInitializeRegion();
        }

        private void OnEnable()
        {
            if (simulation == null)
                TryInitializeRegion();

            ResolveManagerRegistration();
        }

        private void OnDisable()
        {
            UnbindMembers();
            simulationManager?.Unregister(this);
        }

        private void OnTransformParentChanged()
        {
            if (isActiveAndEnabled)
                ResolveManagerRegistration();
        }

        private void OnValidate()
        {
            waterDensity = Mathf.Max(MinimumDensity, waterDensity);
            initialVolume = Mathf.Max(0f, initialVolume);
            cellResolution = Mathf.Max(MinimumCellResolution, cellResolution);
            maximumGridCells = Mathf.Max(1, maximumGridCells);

            if (members == null)
                members = new List<FloodVolume>();

            if (simulationManager == null)
                simulationManager =
                    GetComponentInParent<FloodSimulationManager>();

            TryValidateMembers(out validationMessage);
        }

        /// <summary>
        /// Configures Editor bake resolution settings used by Bake Region.
        /// </summary>
        public void ConfigureBakeSettings(
            float resolutionMeters,
            int maximumCells)
        {
            cellResolution = Mathf.Max(MinimumCellResolution, resolutionMeters);
            maximumGridCells = Mathf.Max(1, maximumCells);
        }

        /// <summary>
        /// Assigns a baked region occupancy asset and rebuilds when enabled.
        /// </summary>
        public void AssignBakedRegionData(FloodRegionData data)
        {
            bakedRegionData = data;

            if (isActiveAndEnabled)
                Rebuild();
        }

        internal void AssignBake(FloodRegionData data)
        {
            bakedRegionData = data;
        }

        /// <summary>
        /// Validates authored members without entering Play Mode.
        /// </summary>
        public bool TryValidateMembers(out string message)
        {
            message = null;

            if (members == null || members.Count == 0)
            {
                message = "FloodRegion requires at least one FloodVolume member.";
                return false;
            }

            var seen = new HashSet<FloodVolume>();

            for (var index = 0; index < members.Count; index++)
            {
                var member = members[index];

                if (member == null)
                {
                    message =
                        $"FloodRegion '{name}' member slot {index} is unassigned.";
                    return false;
                }

                if (!seen.Add(member))
                {
                    message =
                        $"FloodRegion '{name}' lists FloodVolume '{member.name}' more than once.";
                    return false;
                }

                if (Mathf.Abs(member.WaterDensity - waterDensity) > 0.01f)
                {
                    message =
                        $"FloodRegion '{name}' density ({waterDensity} kg/m³) must match "
                        + $"member '{member.name}' ({member.WaterDensity} kg/m³).";
                    return false;
                }
            }

            message = string.Empty;
            return true;
        }

        /// <inheritdoc />
        public FluidBoundarySnapshot CaptureBoundarySnapshot()
        {
            var state = CurrentState;
            return new FluidBoundarySnapshot(
                BoundaryId,
                simulationManager,
                state.SurfacePlane,
                waterDensity,
                hasFiniteSupply: true,
                availableVolume: state.Volume,
                hasFiniteCapacity: true,
                remainingCapacity: Math.Max(0d, state.Capacity - state.Volume),
                acceptsCommits: true,
                isEnabled: IsBoundaryEnabled);
        }

        internal void ApplyManagedVolumeDelta(double cubicMeters)
        {
            if (cubicMeters > 0d)
                simulation?.AddVolume(cubicMeters);
            else if (cubicMeters < 0d)
                simulation?.RemoveVolume(-cubicMeters);
        }

        private bool publishedThisTick;

        internal void BeginTick()
        {
            publishedThisTick = false;
        }

        internal void PublishManagedState()
        {
            if (publishedThisTick)
                return;

            publishedThisTick = true;
            PublishStateChanges();

            for (var index = 0; index < boundMembers.Count; index++)
                boundMembers[index].PublishManagedStateFromRegion();
        }

        /// <summary>
        /// Gets whether this member is the single manager commit participant for
        /// the region (avoids double-counting multi-member water).
        /// </summary>
        internal bool IsCommitParticipant(FloodVolume volume)
        {
            return volume != null
                && boundMembers.Count > 0
                && boundMembers[0] == volume;
        }

        internal void UseManagerIfUnset(FloodSimulationManager manager)
        {
            if (simulationManager == null)
                SetSimulationManager(manager);
            else if (simulationManager == manager && isActiveAndEnabled)
                simulationManager.Register(this);
        }

        internal FloodState CaptureMemberFacadeState(FloodVolume member)
        {
            if (member == null || simulation == null || geometry == null)
                return default;

            // One-member Phase A: solve against the member's geometry/transform
            // for behavioral parity with a standalone FloodVolume.
            if (boundMembers.Count == 1 && boundMembers[0] == member)
                return member.CaptureDelegatedRegionState(this);

            return CaptureState();
        }

        internal double GetAuthoritativeVolume()
        {
            return simulation?.CurrentVolume ?? 0d;
        }

        internal VolumeChangeResult ApplyMemberMutation(double signedCubicMeters)
        {
            if (simulation == null)
                return new VolumeChangeResult(
                    signedCubicMeters,
                    appliedChange: 0d,
                    previousVolume: 0d,
                    currentVolume: 0d);

            if (signedCubicMeters > 0d)
                return simulation.AddVolume(signedCubicMeters);

            if (signedCubicMeters < 0d)
                return simulation.RemoveVolume(-signedCubicMeters);

            return simulation.SetVolume(simulation.CurrentVolume);
        }

        private bool TryInitializeRegion()
        {
            if (members == null || members.Count == 0)
                return false;

            UnbindMembers();

            if (!TryValidateMembers(out validationMessage))
            {
                Debug.LogError(
                    $"FloodRegion '{name}' is invalid: {validationMessage}",
                    this);
                return false;
            }

            if (!TryBuildGeometry(out geometry, out validationMessage))
            {
                Debug.LogError(
                    $"FloodRegion '{name}' geometry failed: {validationMessage}",
                    this);
                return false;
            }

            initialVolume = Mathf.Clamp(
                initialVolume,
                0f,
                (float)geometry.Capacity);

            simulation = FloodSimulationFactory.Create(geometry, initialVolume);
            BindMembers();
            previousState = CaptureState();
            hasPreviousState = true;
            return true;
        }

        private bool TryBuildGeometry(
            out IFloodVolumeGeometry builtGeometry,
            out string message)
        {
            builtGeometry = null;
            message = null;

            if (members.Count == 1)
            {
                var member = members[0];
                var memberGeometry = member.Geometry;

                if (memberGeometry == null)
                {
                    message =
                        $"Member '{member.name}' has invalid geometry.";
                    return false;
                }

                if (!AreTransformsCompatible(member.transform, transform))
                {
                    message =
                        $"FloodRegion '{name}' and member '{member.name}' must share "
                        + "the same transform for one-member / Phase A parity "
                        + "(same GameObject or identity relative transform).";
                    return false;
                }

                builtGeometry = memberGeometry;
                message = string.Empty;
                return true;
            }

            if (!CompositeFloodGeometry.TryCreate(
                    this,
                    members,
                    out builtGeometry,
                    out message))
            {
                return false;
            }

            message = string.Empty;
            return true;
        }

        private void BindMembers()
        {
            boundMembers.Clear();

            for (var index = 0; index < members.Count; index++)
            {
                var member = members[index];
                if (member == null)
                    continue;

                member.BindOwningRegion(this);
                boundMembers.Add(member);
            }
        }

        private void UnbindMembers()
        {
            for (var index = 0; index < boundMembers.Count; index++)
            {
                var member = boundMembers[index];
                if (member != null)
                    member.UnbindOwningRegion(this);
            }

            boundMembers.Clear();
        }

        private FloodState CaptureState()
        {
            var volume = simulation?.CurrentVolume ?? 0d;
            var capacity = simulation?.MaximumVolume
                ?? geometry?.Capacity
                ?? 0d;
            var height = simulation?.CurrentHeight ?? 0d;
            var fillPercentage = simulation?.FillPercentage ?? 0d;
            var solution = geometry == null
                ? default
                : ResolveSurfaceSolution();
            var surfacePlane = geometry == null
                ? new Plane(transform.up, transform.position)
                : FloodPlaneUtility.LocalToWorld(
                    transform,
                    solution.LocalSurfacePlane);
            var centerOfMass = geometry == null
                ? transform.position
                : transform.TransformPoint(solution.Submersion.Centroid);

            return new FloodState(
                volume,
                capacity,
                height,
                fillPercentage,
                simulation?.IsEmpty ?? true,
                simulation?.IsFull ?? false,
                surfacePlane,
                volume * waterDensity,
                centerOfMass);
        }

        private FloodSurfaceSolution ResolveSurfaceSolution()
        {
            var gravity =
                simulationManager == null
                    ? Physics.gravity
                    : simulationManager.ActiveGravity;
            Vector3 localSurfaceNormal;

            if (IsFinite(gravity)
                && gravity.sqrMagnitude
                    >= FloodGeometryTolerances.MinimumGravityMagnitude
                        * FloodGeometryTolerances.MinimumGravityMagnitude)
            {
                localSurfaceNormal =
                    FloodPlaneUtility.WorldNormalToLocal(
                        transform,
                        -gravity.normalized);
                lastValidLocalSurfaceNormal = localSurfaceNormal;
                hasLastValidSurfaceNormal = true;
            }
            else
            {
                localSurfaceNormal = hasLastValidSurfaceNormal
                    ? lastValidLocalSurfaceNormal
                    : Vector3.up;
            }

            var volume = simulation?.CurrentVolume ?? 0d;

            if (hasCachedSurfaceSolution
                && cachedSurfaceVolume.Equals(volume)
                && (cachedSurfaceNormal - localSurfaceNormal).sqrMagnitude
                    <= FloodGeometryTolerances.PlaneNormal
                        * FloodGeometryTolerances.PlaneNormal)
            {
                return cachedSurfaceSolution;
            }

            cachedSurfaceSolution = FloodSurfaceSolver.Solve(
                geometry,
                localSurfaceNormal,
                volume);
            cachedSurfaceVolume = volume;
            cachedSurfaceNormal = localSurfaceNormal;
            hasCachedSurfaceSolution = true;
            return cachedSurfaceSolution;
        }

        private void PublishStateChanges()
        {
            if (simulation == null)
                return;

            var currentState = CaptureState();

            if (hasPreviousState && currentState == previousState)
                return;

            var volumeChanged =
                !hasPreviousState
                || currentState.Volume != previousState.Volume;

            var heightChanged =
                !hasPreviousState
                || currentState.Height != previousState.Height;

            previousState = currentState;
            hasPreviousState = true;

            StateChanged?.Invoke(currentState);

            if (volumeChanged)
                VolumeChanged?.Invoke(currentState.Volume);

            if (heightChanged)
                WaterHeightChanged?.Invoke((float)currentState.Height);
        }

        private void ResolveManagerRegistration()
        {
            if (simulationManager == null)
                simulationManager =
                    GetComponentInParent<FloodSimulationManager>();

            if (isActiveAndEnabled)
                simulationManager?.Register(this);
        }

        private void SetSimulationManager(FloodSimulationManager manager)
        {
            if (simulationManager == manager)
                return;

            simulationManager?.Unregister(this);
            simulationManager = manager;

            if (isActiveAndEnabled)
                simulationManager?.Register(this);
        }

        private VolumeChangeResult CreateUnavailableResult(
            double requestedChange)
        {
            return new VolumeChangeResult(
                requestedChange,
                appliedChange: 0d,
                previousVolume: CurrentVolume,
                currentVolume: CurrentVolume);
        }

        private static bool AreTransformsCompatible(
            Transform memberTransform,
            Transform regionTransform)
        {
            if (memberTransform == regionTransform)
                return true;

            var localPosition = regionTransform.InverseTransformPoint(
                memberTransform.position);
            var localRotation = Quaternion.Inverse(regionTransform.rotation)
                * memberTransform.rotation;

            return localPosition.sqrMagnitude
                    <= FloodGeometryTolerances.Position
                        * FloodGeometryTolerances.Position
                && Quaternion.Angle(localRotation, Quaternion.identity)
                    <= 0.01f
                && (memberTransform.lossyScale - regionTransform.lossyScale)
                    .sqrMagnitude
                    <= FloodGeometryTolerances.Position
                        * FloodGeometryTolerances.Position;
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
    }
}
