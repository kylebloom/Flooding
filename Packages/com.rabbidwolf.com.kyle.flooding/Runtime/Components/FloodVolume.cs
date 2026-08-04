using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Kyle.Flooding
{
    /// <summary>
    /// Owns the flooding state and authored geometry for one scene compartment.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FloodVolume : MonoBehaviour, IMassContributor, IFluidBoundary
    {
        private const float MinimumDimension = 0.01f;
        private const float MinimumDensity = 0.01f;

        [Header("Simulation")]

        [SerializeField]
        [Tooltip("Manager that advances and publishes this volume. If unassigned, the nearest parent manager is used.")]
        private FloodSimulationManager simulationManager;

        [Header("Floodable Space")]

        [SerializeField]
        [Tooltip("Container shape used for capacity, fill height, center of mass, and surface geometry.")]
        private FloodGeometryMode geometryMode;

        [SerializeField]
        [Tooltip("Rectangular interior width in meters along local X. Used only in Rectangular Prism mode.")]
        [Min(MinimumDimension)]
        private float width = 5f;

        [SerializeField]
        [Tooltip("Rectangular interior length in meters along local Z. Used only in Rectangular Prism mode.")]
        [Min(MinimumDimension)]
        private float length = 5f;

        [SerializeField]
        [Tooltip("Ordered local XZ perimeter points for Extruded Polygon mode. Concave simple polygons are supported; holes and self-intersections are not.")]
        private Vector2[] polygonFootprint =
        {
            new Vector2(-2.5f, -2.5f),
            new Vector2(2.5f, -2.5f),
            new Vector2(2.5f, 2.5f),
            new Vector2(-2.5f, 2.5f),
        };

        [SerializeField]
        [Tooltip("Maximum water height in meters along local Y.")]
        [Min(MinimumDimension)]
        private float maximumHeight = 3f;

        [SerializeField]
        [Tooltip("Immutable Flood Volume Data asset used only in Baked Data mode. Create and update it with Flood Volume Authoring in the Unity Editor.")]
        private FloodVolumeData bakedVolumeData;

        [Header("Fluid")]

        [SerializeField]
        [Tooltip("Water density in kilograms per cubic meter. Fresh water is approximately 1000 kg/m³.")]
        [Min(MinimumDensity)]
        private float waterDensity = 1000f;

        [Header("Initial State")]

        [SerializeField]
        [Tooltip("Water volume present when Play Mode begins, in cubic meters. Values above capacity are clamped.")]
        [Min(0f)]
        private float initialVolume;

        [SerializeField]
        [HideInInspector]
        [FormerlySerializedAs("initialWaterHeight")]
        private float legacyInitialWaterHeight = -1f;

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

        /// <summary>
        /// Gets or sets the manager that advances and publishes this volume.
        /// </summary>
        public FloodSimulationManager SimulationManager
        {
            get => simulationManager;
            set => SetSimulationManager(value);
        }

        /// <summary>
        /// Gets the selected authored geometry mode.
        /// </summary>
        public FloodGeometryMode GeometryMode => geometryMode;

        /// <summary>
        /// Gets the active immutable geometry abstraction.
        /// </summary>
        public IFloodVolumeGeometry Geometry
        {
            get
            {
                if (geometry != null)
                    return geometry;

                return TryCreateAuthoredGeometry(out var created, out _)
                    ? created
                    : null;
            }
        }

        /// <summary>
        /// Gets the authored polygon footprint in local XZ coordinates.
        /// </summary>
        public IReadOnlyList<Vector2> PolygonFootprint =>
            polygonFootprint ?? Array.Empty<Vector2>();

        /// <summary>
        /// Gets the compartment local-X bounds width in meters.
        /// </summary>
        public float Width => GetAuthoredBounds().size.x;

        /// <summary>
        /// Gets the compartment local-Z bounds length in meters.
        /// </summary>
        public float Length => GetAuthoredBounds().size.z;

        /// <summary>
        /// Gets the constant footprint area in square meters.
        /// </summary>
        public float FloorArea =>
            geometry is IExtrudedFloodVolumeGeometry activeGeometry
                ? (float)activeGeometry.FloorArea
                : geometry != null
                    ? (float)GetEquivalentFloorArea(geometry)
                : TryGetAuthoredFloorArea(out var area)
                    ? (float)area
                    : 0f;

        /// <summary>
        /// Gets the maximum water height in meters.
        /// </summary>
        public float MaximumHeight =>
            geometryMode == FloodGeometryMode.BakedData
                ? GetAuthoredBounds().size.y
                : maximumHeight;

        /// <summary>
        /// Gets the immutable baked asset selected for Baked Data mode.
        /// </summary>
        public FloodVolumeData BakedVolumeData => bakedVolumeData;

        /// <summary>
        /// Gets the authored initial water volume in cubic meters.
        /// </summary>
        public float InitialVolume => initialVolume;

        /// <summary>
        /// Gets the configured water density in kilograms per cubic meter.
        /// </summary>
        public float WaterDensity => waterDensity;

        /// <inheritdoc />
        public FluidBoundaryId BoundaryId => FluidBoundaryId.FromObject(this);

        /// <inheritdoc />
        public bool IsBoundaryEnabled => isActiveAndEnabled;

        /// <summary>
        /// Gets the equivalent level-fill height in meters. A tilted surface
        /// does not generally pass through this local-Y value.
        /// </summary>
        public float CurrentHeight =>
            simulation == null
                ? 0f
                : (float)simulation.CurrentHeight;

        /// <summary>
        /// Gets the current water volume in cubic meters.
        /// </summary>
        public float CurrentVolume =>
            simulation == null
                ? 0f
                : (float)simulation.CurrentVolume;

        /// <summary>
        /// Gets the compartment capacity in cubic meters.
        /// </summary>
        public float MaximumVolume =>
            simulation == null
                ? (float)(Geometry?.Capacity ?? 0d)
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
        /// Gets the solved water surface plane in compartment-local space.
        /// </summary>
        public Plane LocalSurfacePlane =>
            geometry == null
                ? new Plane(Vector3.up, Vector3.zero)
                : ResolveSurfaceSolution().LocalSurfacePlane;

        /// <summary>
        /// Gets the signed solved volume error in cubic meters.
        /// </summary>
        public double SurfaceVolumeError =>
            geometry == null
                ? 0d
                : ResolveSurfaceSolution().VolumeError;

        /// <summary>
        /// Gets the iterations used by the latest surface solve.
        /// </summary>
        public int SurfaceSolveIterations =>
            geometry == null
                ? 0
                : ResolveSurfaceSolution().Iterations;

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
        /// Returns whether a world-space point lies inside this compartment's
        /// floodable geometry. Uses the current authoritative geometry and never
        /// advances simulation. Baked volumes use occupancy-cell approximation;
        /// see <see cref="IFloodVolumeGeometry.ContainmentPrecision"/>.
        /// </summary>
        public bool ContainsPoint(Vector3 worldPoint)
        {
            return QueryPoint(worldPoint).IsInsideVolume;
        }

        /// <summary>
        /// Returns whether a world-space point is inside this compartment and
        /// below the current water surface plane. Uses live authoritative state
        /// and never advances simulation.
        /// </summary>
        public bool IsPointSubmerged(Vector3 worldPoint)
        {
            return QueryPoint(worldPoint).IsSubmerged;
        }

        /// <summary>
        /// Queries submersion and surface data for a world-space sample point.
        /// Values are derived from this volume's current authoritative state at
        /// the moment of the call. The query is read-only and never advances,
        /// reconciles, or publishes simulation state.
        /// </summary>
        public FloodQueryResult QueryPoint(Vector3 worldPoint)
        {
            var activeGeometry = Geometry;
            var isInsideVolume = activeGeometry != null
                && activeGeometry.ContainsLocalPoint(
                    transform.InverseTransformPoint(worldPoint));

            var surfacePlane = SurfacePlane;
            var submersionDepth = Mathf.Max(
                0f,
                -surfacePlane.GetDistanceToPoint(worldPoint));
            var isSubmerged = isInsideVolume && submersionDepth > 0f;

            return new FloodQueryResult(
                isInsideVolume,
                isSubmerged,
                isSubmerged ? submersionDepth : 0f,
                surfacePlane.ClosestPointOnPlane(worldPoint),
                surfacePlane.normal);
        }

        /// <summary>
        /// Raised after the manager publishes a flood state change.
        /// Direct volume mutations update authoritative state immediately but
        /// raise this event on the next publish, not at mutation time.
        /// </summary>
        public event Action<FloodState> StateChanged;

        /// <summary>
        /// Raised after the current volume changes.
        /// </summary>
        public event Action<double> VolumeChanged;

        /// <summary>
        /// Raised after the equivalent level-fill height changes.
        /// Retained for compatibility with the prototype renderer.
        /// </summary>
        public event Action<float> WaterHeightChanged;

        private void Awake()
        {
            MigrateLegacyInitialHeight();
            if (!InitializeSimulation())
                return;

            previousState = CaptureState();
            hasPreviousState = true;
            ResolveManagerRegistration();
        }

        private void OnEnable()
        {
            ResolveManagerRegistration();
        }

        private void OnDisable()
        {
            simulationManager?.Unregister(this);
        }

        private void OnTransformParentChanged()
        {
            if (isActiveAndEnabled)
                ResolveManagerRegistration();
        }

        private void OnValidate()
        {
            width = Mathf.Max(MinimumDimension, width);
            length = Mathf.Max(MinimumDimension, length);
            maximumHeight = Mathf.Max(MinimumDimension, maximumHeight);
            waterDensity = Mathf.Max(MinimumDensity, waterDensity);

            MigrateLegacyInitialHeight();

            if (TryCreateAuthoredGeometry(
                    out var authoredGeometry,
                    out _))
            {
                initialVolume = Mathf.Clamp(
                    initialVolume,
                    0f,
                    (float)authoredGeometry.Capacity);
            }

            if (simulationManager == null)
                simulationManager = GetComponentInParent<FloodSimulationManager>();
        }

        /// <summary>
        /// Validates the currently authored geometry without entering Play Mode.
        /// </summary>
        public bool TryValidateGeometry(out string message)
        {
            return TryCreateAuthoredGeometry(out _, out message);
        }

        /// <summary>
        /// Configures centered rectangular geometry and preserves as much current
        /// volume as the new capacity permits.
        /// </summary>
        public void ConfigureRectangularGeometry(
            float newWidth,
            float newLength,
            float newMaximumHeight)
        {
            var newGeometry = new RectangularPrismFloodGeometry(
                newWidth,
                newLength,
                newMaximumHeight);

            geometryMode = FloodGeometryMode.RectangularPrism;
            width = newWidth;
            length = newLength;
            maximumHeight = newMaximumHeight;
            RebuildSimulation(newGeometry);
        }

        /// <summary>
        /// Configures a simple polygon prism and preserves as much current volume
        /// as the new capacity permits.
        /// </summary>
        public void ConfigurePolygonGeometry(
            IReadOnlyList<Vector2> newFootprint,
            float newMaximumHeight)
        {
            var newGeometry = new ExtrudedPolygonFloodGeometry(
                newFootprint,
                newMaximumHeight);
            var copiedFootprint = new Vector2[newFootprint.Count];

            for (var index = 0; index < newFootprint.Count; index++)
                copiedFootprint[index] = newFootprint[index];

            geometryMode = FloodGeometryMode.ExtrudedPolygon;
            polygonFootprint = copiedFootprint;
            maximumHeight = newMaximumHeight;
            RebuildSimulation(newGeometry);
        }

        /// <summary>
        /// Selects immutable Editor-baked geometry and preserves as much
        /// current volume as the baked capacity permits.
        /// </summary>
        public void ConfigureBakedGeometry(FloodVolumeData data)
        {
            var newGeometry = new BakedFloodGeometry(data);
            geometryMode = FloodGeometryMode.BakedData;
            bakedVolumeData = data;
            RebuildSimulation(newGeometry);
        }

        /// <summary>
        /// Configures the fluid density used to derive water mass.
        /// </summary>
        /// <param name="kilogramsPerCubicMeter">
        /// Positive finite density in kilograms per cubic meter.
        /// </param>
        public void ConfigureFluidDensity(float kilogramsPerCubicMeter)
        {
            if (float.IsNaN(kilogramsPerCubicMeter)
                || float.IsInfinity(kilogramsPerCubicMeter)
                || kilogramsPerCubicMeter < MinimumDensity)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(kilogramsPerCubicMeter),
                    kilogramsPerCubicMeter,
                    $"Density must be finite and at least {MinimumDensity} kg/m³.");
            }

            waterDensity = kilogramsPerCubicMeter;
        }

        /// <summary>
        /// Attempts to add a volume of water.
        /// </summary>
        /// <param name="cubicMeters">Requested volume in cubic meters.</param>
        /// <returns>The requested, accepted, and rejected volume change.</returns>
        public VolumeChangeResult AddWater(float cubicMeters)
        {
            return simulation == null
                ? CreateUnavailableResult(Math.Max(0f, cubicMeters))
                : simulation.AddVolume(cubicMeters);
        }

        /// <summary>
        /// Attempts to remove a volume of water.
        /// </summary>
        /// <param name="cubicMeters">Requested volume in cubic meters.</param>
        /// <returns>The requested, accepted, and rejected volume change.</returns>
        public VolumeChangeResult RemoveWater(float cubicMeters)
        {
            return simulation == null
                ? CreateUnavailableResult(-Math.Max(0f, cubicMeters))
                : simulation.RemoveVolume(cubicMeters);
        }

        /// <summary>
        /// Applies configured inflow over a time interval.
        /// </summary>
        public VolumeChangeResult AddWaterOverTime(
            float cubicMetersPerSecond,
            float deltaTime)
        {
            return simulation == null
                ? CreateUnavailableResult(
                    Math.Max(0f, cubicMetersPerSecond)
                    * Math.Max(0f, deltaTime))
                : simulation.Step(
                    deltaTime,
                    cubicMetersPerSecond,
                    0d);
        }

        /// <summary>
        /// Applies configured outflow over a time interval.
        /// </summary>
        public VolumeChangeResult RemoveWaterOverTime(
            float cubicMetersPerSecond,
            float deltaTime)
        {
            return simulation == null
                ? CreateUnavailableResult(
                    -Math.Max(0f, cubicMetersPerSecond)
                    * Math.Max(0f, deltaTime))
                : simulation.Step(
                    deltaTime,
                    0d,
                    cubicMetersPerSecond);
        }

        private bool InitializeSimulation()
        {
            if (!TryCreateAuthoredGeometry(
                    out geometry,
                    out var validationMessage))
            {
                Debug.LogError(
                    $"FloodVolume '{name}' has invalid geometry: "
                    + validationMessage,
                    this);
                enabled = false;
                return false;
            }

            simulation = CreateSimulation(geometry, initialVolume);

            return true;
        }

        private void RebuildSimulation(IFloodVolumeGeometry newGeometry)
        {
            var preservedVolume = simulation?.CurrentVolume ?? initialVolume;
            geometry = newGeometry;
            hasCachedSurfaceSolution = false;
            simulation = CreateSimulation(geometry, preservedVolume);

            if (!enabled)
                enabled = true;
        }

        private void MigrateLegacyInitialHeight()
        {
            if (legacyInitialWaterHeight < 0f)
                return;

            var clampedHeight = Mathf.Clamp(
                legacyInitialWaterHeight,
                0f,
                maximumHeight);

            initialVolume = FloorArea * clampedHeight;
            legacyInitialWaterHeight = -1f;
        }

        private FloodState CaptureState()
        {
            var volume = simulation?.CurrentVolume ?? 0d;
            var capacity = simulation?.MaximumVolume ?? MaximumVolume;
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

        internal void PublishManagedState()
        {
            PublishStateChanges();
        }

        internal void UseManagerIfUnset(FloodSimulationManager manager)
        {
            if (simulationManager == null)
                SetSimulationManager(manager);
            else if (simulationManager == manager && isActiveAndEnabled)
                simulationManager.Register(this);
        }

        private void ResolveManagerRegistration()
        {
            if (simulationManager == null)
                simulationManager = GetComponentInParent<FloodSimulationManager>();

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

        private VolumeChangeResult CreateUnavailableResult(
            double requestedChange)
        {
            return new VolumeChangeResult(
                requestedChange,
                appliedChange: 0d,
                previousVolume: CurrentVolume,
                currentVolume: CurrentVolume);
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

        private bool TryCreateAuthoredGeometry(
            out IFloodVolumeGeometry authoredGeometry,
            out string message)
        {
            authoredGeometry = null;

            try
            {
                switch (geometryMode)
                {
                    case FloodGeometryMode.RectangularPrism:
                        authoredGeometry =
                            new RectangularPrismFloodGeometry(
                                width,
                                length,
                                maximumHeight);
                        break;

                    case FloodGeometryMode.ExtrudedPolygon:
                        authoredGeometry =
                            new ExtrudedPolygonFloodGeometry(
                                polygonFootprint,
                                maximumHeight);
                        break;

                    case FloodGeometryMode.BakedData:
                        if (bakedVolumeData == null)
                        {
                            message =
                                "Baked Data mode requires a Flood Volume Data "
                                + "asset. Add Flood Volume Authoring and bake a "
                                + "closed source mesh in the Unity Editor.";
                            return false;
                        }

                        authoredGeometry =
                            new BakedFloodGeometry(bakedVolumeData);
                        break;

                    default:
                        message =
                            $"Geometry mode '{geometryMode}' is not supported.";
                        return false;
                }
            }
            catch (ArgumentException exception)
            {
                message = exception.Message;
                return false;
            }

            message = string.Empty;
            return true;
        }

        private bool TryGetAuthoredFloorArea(out double area)
        {
            if (TryCreateAuthoredGeometry(
                    out var authoredGeometry,
                    out _))
            {
                area = authoredGeometry
                    is IExtrudedFloodVolumeGeometry extrudedGeometry
                        ? extrudedGeometry.FloorArea
                        : GetEquivalentFloorArea(authoredGeometry);
                return true;
            }

            area = 0d;
            return false;
        }

        private Bounds GetAuthoredBounds()
        {
            if (geometry != null)
                return geometry.LocalBounds;

            return TryCreateAuthoredGeometry(
                out var authoredGeometry,
                out _)
                    ? authoredGeometry.LocalBounds
                    : default;
        }

        private static FloodSimulation CreateSimulation(
            IFloodVolumeGeometry sourceGeometry,
            double volume)
        {
            var height = Math.Max(
                MinimumDimension,
                sourceGeometry.LocalBounds.size.y);
            return new FloodSimulation(
                GetEquivalentFloorArea(sourceGeometry),
                height,
                volume);
        }

        private static double GetEquivalentFloorArea(
            IFloodVolumeGeometry sourceGeometry)
        {
            var height = Math.Max(
                MinimumDimension,
                sourceGeometry.LocalBounds.size.y);
            return sourceGeometry.Capacity / height;
        }

        private void DrawPolygonGizmo()
        {
            if (polygonFootprint == null || polygonFootprint.Length < 2)
                return;

            for (var index = 0; index < polygonFootprint.Length; index++)
            {
                var next = (index + 1) % polygonFootprint.Length;
                var floorStart = new Vector3(
                    polygonFootprint[index].x,
                    0f,
                    polygonFootprint[index].y);
                var floorEnd = new Vector3(
                    polygonFootprint[next].x,
                    0f,
                    polygonFootprint[next].y);
                var ceilingStart =
                    floorStart + (Vector3.up * maximumHeight);
                var ceilingEnd =
                    floorEnd + (Vector3.up * maximumHeight);

                Gizmos.DrawLine(floorStart, floorEnd);
                Gizmos.DrawLine(ceilingStart, ceilingEnd);
                Gizmos.DrawLine(floorStart, ceilingStart);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;

            if (geometryMode == FloodGeometryMode.RectangularPrism)
            {
                Gizmos.DrawWireCube(
                    new Vector3(
                        0f,
                        maximumHeight * 0.5f,
                        0f),
                    new Vector3(
                        width,
                        maximumHeight,
                        length));
                return;
            }

            if (geometryMode == FloodGeometryMode.ExtrudedPolygon)
                DrawPolygonGizmo();
        }
    }
}
