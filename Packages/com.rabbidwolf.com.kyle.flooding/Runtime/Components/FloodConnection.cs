using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Kyle.Flooding
{
    /// <summary>
    /// Represents a managed bidirectional opening between two fluid boundaries.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FloodConnection : MonoBehaviour
    {
        private const float MinimumOpeningDimension = 0.01f;

        [Header("Simulation")]

        [SerializeField]
        [Tooltip("Manager that evaluates this connection. Both endpoints must use the same manager.")]
        private FloodSimulationManager simulationManager;

        [Header("Connected Boundaries")]

        [SerializeField]
        [Tooltip("Fluid boundary on side A. Assign a FloodVolume or External Fluid Body. Positive flow travels from A to B.")]
        private FluidBoundaryReference sideA;

        [SerializeField]
        [Tooltip("Fluid boundary on side B. Assign a FloodVolume or External Fluid Body. Negative flow travels from B to A.")]
        private FluidBoundaryReference sideB;

        [SerializeField]
        [HideInInspector]
        [FormerlySerializedAs("volumeA")]
        private FloodVolume legacyVolumeA;

        [SerializeField]
        [HideInInspector]
        [FormerlySerializedAs("volumeB")]
        private FloodVolume legacyVolumeB;

        [Header("Opening")]

        [SerializeField]
        [Tooltip("Opening width in meters along this Transform's local X axis.")]
        [Min(MinimumOpeningDimension)]
        private float openingWidth = 1f;

        [SerializeField]
        [Tooltip("Opening height in meters above this Transform's position along local Y.")]
        [Min(MinimumOpeningDimension)]
        private float openingHeight = 2f;

        [SerializeField]
        [Tooltip("Dimensionless orifice discharge coefficient from zero to one. A typical doorway approximation is 0.62.")]
        [Range(0f, 1f)]
        private float dischargeCoefficient = 0.62f;

        [SerializeField]
        [Tooltip("Whether water may currently flow through this opening.")]
        private bool isOpen = true;

        [Header("Presentation")]

        [SerializeField]
        [Tooltip("Optional Transform where ingress visuals spawn for water entering a destination volume. When unset, Opening Center World is used. Simulation ignores this field.")]
        private Transform ingressAnchor;

        /// <summary>
        /// Gets or sets the manager that evaluates this connection.
        /// </summary>
        public FloodSimulationManager SimulationManager
        {
            get => simulationManager;
            set => SetSimulationManager(value);
        }

        /// <summary>
        /// Gets or sets the fluid boundary on side A.
        /// </summary>
        public IFluidBoundary SideA
        {
            get => sideA.TryGet(out var boundary) ? boundary : null;
            set
            {
                sideA = FluidBoundaryReference.From(value);
                ResolveManagerRegistration();
            }
        }

        /// <summary>
        /// Gets or sets the fluid boundary on side B.
        /// </summary>
        public IFluidBoundary SideB
        {
            get => sideB.TryGet(out var boundary) ? boundary : null;
            set
            {
                sideB = FluidBoundaryReference.From(value);
                ResolveManagerRegistration();
            }
        }

        /// <summary>
        /// Gets or sets the finite volume on side A when that side is a
        /// <see cref="FloodVolume"/>.
        /// </summary>
        public FloodVolume VolumeA
        {
            get => SideA as FloodVolume;
            set => SideA = value;
        }

        /// <summary>
        /// Gets or sets the finite volume on side B when that side is a
        /// <see cref="FloodVolume"/>.
        /// </summary>
        public FloodVolume VolumeB
        {
            get => SideB as FloodVolume;
            set => SideB = value;
        }

        /// <summary>
        /// Gets or sets the opening width in meters.
        /// </summary>
        public float OpeningWidth
        {
            get => openingWidth;
            set
            {
                EnsureFinite(value, nameof(value));
                openingWidth = Mathf.Max(MinimumOpeningDimension, value);
            }
        }

        /// <summary>
        /// Gets or sets the opening height in meters.
        /// </summary>
        public float OpeningHeight
        {
            get => openingHeight;
            set
            {
                EnsureFinite(value, nameof(value));
                openingHeight = Mathf.Max(MinimumOpeningDimension, value);
            }
        }

        /// <summary>
        /// Gets or sets the dimensionless discharge coefficient.
        /// </summary>
        public float DischargeCoefficient
        {
            get => dischargeCoefficient;
            set
            {
                EnsureFinite(value, nameof(value));
                dischargeCoefficient = Mathf.Clamp01(value);
            }
        }

        /// <summary>
        /// Gets or sets whether this opening permits flow.
        /// </summary>
        public bool IsOpen
        {
            get => isOpen;
            set => isOpen = value;
        }

        /// <summary>
        /// Gets the unconstrained signed flow requested during the latest tick,
        /// in cubic meters per second.
        /// </summary>
        public double RequestedFlowRate { get; private set; }

        /// <summary>
        /// Gets the capacity- and availability-constrained signed flow applied
        /// during the latest tick, in cubic meters per second.
        /// </summary>
        public double CurrentFlowRate { get; private set; }

        /// <summary>
        /// Gets the source-side submerged opening area from the latest tick, in
        /// square meters.
        /// </summary>
        public double SubmergedOpeningArea { get; private set; }

        /// <summary>
        /// Gets the signed pressure-head difference from the latest tick, in
        /// meters.
        /// </summary>
        public double PressureHeadDifference { get; private set; }

        /// <summary>
        /// Gets the latest authoring or evaluation status message, if any.
        /// </summary>
        public string ValidationMessage { get; private set; }

        /// <summary>
        /// Gets the latest flow direction in world space.
        /// </summary>
        public Vector3 FlowDirectionWorld =>
            CurrentFlowRate > 0d
                ? transform.forward
                : CurrentFlowRate < 0d
                    ? -transform.forward
                    : Vector3.zero;

        /// <summary>
        /// Gets or sets an optional presentation-only ingress anchor Transform.
        /// Simulation ignores this field.
        /// </summary>
        public Transform IngressAnchor
        {
            get => ingressAnchor;
            set => ingressAnchor = value;
        }

        /// <summary>
        /// Gets the world-space center of the rectangular opening
        /// (bottom center + half opening height along local Y).
        /// </summary>
        public Vector3 OpeningCenterWorld =>
            transform.TransformPoint(new Vector3(0f, openingHeight * 0.5f, 0f));

        /// <summary>
        /// Gets the preferred world-space ingress presentation position.
        /// Uses <see cref="IngressAnchor"/> when assigned; otherwise
        /// <see cref="OpeningCenterWorld"/>.
        /// </summary>
        public Vector3 IngressWorldPosition =>
            ingressAnchor != null ? ingressAnchor.position : OpeningCenterWorld;

        private void Awake()
        {
            MigrateLegacyEndpoints();
            ResolveManagerRegistration();
        }

        private void OnEnable()
        {
            MigrateLegacyEndpoints();
            ResolveManagerRegistration();
        }

        private void OnDisable()
        {
            simulationManager?.Unregister(this);
            ResetTickState();
        }

        private void OnTransformParentChanged()
        {
            if (isActiveAndEnabled)
                ResolveManagerRegistration();
        }

        private void OnValidate()
        {
            openingWidth = SanitizePositive(
                openingWidth,
                MinimumOpeningDimension);
            openingHeight = SanitizePositive(
                openingHeight,
                MinimumOpeningDimension);
            dischargeCoefficient =
                float.IsNaN(dischargeCoefficient)
                || float.IsInfinity(dischargeCoefficient)
                    ? 0.62f
                    : Mathf.Clamp01(dischargeCoefficient);

            MigrateLegacyEndpoints();

            if (simulationManager == null)
                simulationManager = ResolveEndpointManager();

            TryValidateEndpoints(out var message);
            ValidationMessage = message;
        }

        internal bool TryEvaluate(
            FloodSimulationManager manager,
            IReadOnlyDictionary<FluidBoundaryId, FluidBoundarySnapshot> snapshots,
            double deltaTime,
            double gravityMagnitude,
            out FloodConnectionEvaluation evaluation)
        {
            evaluation = default;
            ValidationMessage = null;

            if (
                !isActiveAndEnabled
                || !isOpen
                || simulationManager != manager)
            {
                ResetTickState();
                return false;
            }

            if (!TryResolveEndpoints(
                    manager,
                    snapshots,
                    out var snapshotA,
                    out var snapshotB,
                    out var finiteA,
                    out var finiteB,
                    out var message))
            {
                ValidationMessage = message;
                ResetTickState();
                return false;
            }

            var openingBottom = transform.position;
            var pressureHeadA = Math.Max(
                0d,
                -snapshotA.SurfacePlane.GetDistanceToPoint(openingBottom));
            var pressureHeadB = Math.Max(
                0d,
                -snapshotB.SurfacePlane.GetDistanceToPoint(openingBottom));

            var flow = FloodFlowCalculator.Calculate(
                pressureHeadA,
                pressureHeadB,
                openingWidth,
                openingHeight,
                dischargeCoefficient,
                gravityMagnitude);

            var requestedVolume =
                Math.Abs(flow.SignedFlowRate) * deltaTime;

            if (double.IsInfinity(requestedVolume))
                requestedVolume = double.MaxValue;

            var sourceIsA = flow.SignedFlowRate > 0d;
            evaluation = new FloodConnectionEvaluation(
                this,
                flow,
                sourceIsA ? snapshotA.BoundaryId : snapshotB.BoundaryId,
                sourceIsA ? snapshotB.BoundaryId : snapshotA.BoundaryId,
                sourceIsA ? finiteA : finiteB,
                sourceIsA ? finiteB : finiteA,
                sourceIsA ? snapshotA : snapshotB,
                sourceIsA ? snapshotB : snapshotA,
                requestedVolume);

            return true;
        }

        internal void ApplyTickResult(
            FloodFlowResult result,
            double appliedSignedFlowRate)
        {
            RequestedFlowRate = result.SignedFlowRate;
            CurrentFlowRate = appliedSignedFlowRate;
            SubmergedOpeningArea = result.SubmergedOpeningArea;
            PressureHeadDifference = result.PressureHeadDifference;
        }

        internal void UseManagerIfUnset(FloodSimulationManager manager)
        {
            if (simulationManager == null)
                SetSimulationManager(manager);
            else if (simulationManager == manager && isActiveAndEnabled)
                simulationManager.Register(this);
        }

        internal bool TryValidateEndpoints(out string message)
        {
            message = null;

            if (!sideA.TryGet(out var boundaryA))
            {
                message = "Side A must reference a FloodVolume or External Fluid Body.";
                return false;
            }

            if (!sideB.TryGet(out var boundaryB))
            {
                message = "Side B must reference a FloodVolume or External Fluid Body.";
                return false;
            }

            if (boundaryA.BoundaryId == boundaryB.BoundaryId)
            {
                message = "Both endpoints resolve to the same fluid boundary.";
                return false;
            }

            var externalA = boundaryA is ExternalFluidBoundary;
            var externalB = boundaryB is ExternalFluidBoundary;

            if (externalA && externalB)
            {
                message =
                    "Connecting two external fluid boundaries is unsupported.";
                return false;
            }

            if (!externalA && !externalB && (boundaryA is not FloodVolume || boundaryB is not FloodVolume))
            {
                message =
                    "Each endpoint must be a FloodVolume or External Fluid Body.";
                return false;
            }

            if (
                boundaryA.SimulationManager != null
                && boundaryB.SimulationManager != null
                && boundaryA.SimulationManager != boundaryB.SimulationManager)
            {
                message = "Both endpoints must belong to the same FloodSimulationManager.";
                return false;
            }

            if (
                simulationManager != null
                && (
                    (
                        boundaryA.SimulationManager != null
                        && boundaryA.SimulationManager != simulationManager)
                    || (
                        boundaryB.SimulationManager != null
                        && boundaryB.SimulationManager != simulationManager)))
            {
                message =
                    "Connection and endpoint managers must match.";
                return false;
            }

            var densityA = boundaryA is FloodVolume volumeA
                ? volumeA.WaterDensity
                : ((ExternalFluidBoundary)boundaryA).Density;
            var densityB = boundaryB is FloodVolume volumeB
                ? volumeB.WaterDensity
                : ((ExternalFluidBoundary)boundaryB).Density;

            if (!FloodFluidTolerances.DensitiesMatch(densityA, densityB))
            {
                message =
                    "Connected fluids must use matching density within "
                    + $"{FloodFluidTolerances.DensityAbsolute} kg/m³ absolute "
                    + $"or {FloodFluidTolerances.DensityRelative} relative tolerance.";
                return false;
            }

            return true;
        }

        private bool TryResolveEndpoints(
            FloodSimulationManager manager,
            IReadOnlyDictionary<FluidBoundaryId, FluidBoundarySnapshot> snapshots,
            out FluidBoundarySnapshot snapshotA,
            out FluidBoundarySnapshot snapshotB,
            out FloodVolume finiteA,
            out FloodVolume finiteB,
            out string message)
        {
            snapshotA = default;
            snapshotB = default;
            finiteA = null;
            finiteB = null;
            message = null;

            if (!TryValidateEndpoints(out message))
                return false;

            sideA.TryGet(out var boundaryA);
            sideB.TryGet(out var boundaryB);

            if (
                !snapshots.TryGetValue(boundaryA.BoundaryId, out snapshotA)
                || !snapshots.TryGetValue(boundaryB.BoundaryId, out snapshotB)
                || !snapshotA.IsEnabled
                || !snapshotB.IsEnabled
                || snapshotA.Owner != manager
                || snapshotB.Owner != manager)
            {
                message =
                    "One or both endpoints are missing from the manager snapshot.";
                return false;
            }

            if (
                !FloodFluidTolerances.DensitiesMatch(
                    snapshotA.DensityKgPerCubicMeter,
                    snapshotB.DensityKgPerCubicMeter))
            {
                message = "Connected fluids must use matching density.";
                return false;
            }

            finiteA = boundaryA as FloodVolume;
            finiteB = boundaryB as FloodVolume;
            return true;
        }

        private void MigrateLegacyEndpoints()
        {
            if (legacyVolumeA != null && !sideA.IsAssigned)
            {
                sideA = FluidBoundaryReference.From(legacyVolumeA);
                legacyVolumeA = null;
            }

            if (legacyVolumeB != null && !sideB.IsAssigned)
            {
                sideB = FluidBoundaryReference.From(legacyVolumeB);
                legacyVolumeB = null;
            }
        }

        private void ResolveManagerRegistration()
        {
            if (simulationManager == null)
                simulationManager = ResolveEndpointManager();

            if (isActiveAndEnabled)
                simulationManager?.Register(this);
        }

        private FloodSimulationManager ResolveEndpointManager()
        {
            if (
                sideA.TryGet(out var boundaryA)
                && boundaryA.SimulationManager != null
                && (
                    !sideB.TryGet(out var boundaryB)
                    || boundaryB.SimulationManager == null
                    || boundaryB.SimulationManager == boundaryA.SimulationManager))
            {
                return boundaryA.SimulationManager;
            }

            if (sideB.TryGet(out var resolvedB) && resolvedB.SimulationManager != null)
                return resolvedB.SimulationManager;

            return GetComponentInParent<FloodSimulationManager>();
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

        private void ResetTickState()
        {
            RequestedFlowRate = 0d;
            CurrentFlowRate = 0d;
            SubmergedOpeningArea = 0d;
            PressureHeadDifference = 0d;
        }

        private static float SanitizePositive(float value, float minimum)
        {
            return
                float.IsNaN(value)
                || float.IsInfinity(value)
                    ? minimum
                    : Mathf.Max(minimum, value);
        }

        private static void EnsureFinite(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentOutOfRangeException(parameterName);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;

            Gizmos.DrawWireCube(
                new Vector3(0f, openingHeight * 0.5f, 0f),
                new Vector3(openingWidth, openingHeight, 0f));
        }
    }

    internal readonly struct FloodConnectionEvaluation
    {
        public FloodConnectionEvaluation(
            FloodConnection connection,
            FloodFlowResult flow,
            FluidBoundaryId sourceId,
            FluidBoundaryId destinationId,
            FloodVolume finiteSource,
            FloodVolume finiteDestination,
            FluidBoundarySnapshot sourceSnapshot,
            FluidBoundarySnapshot destinationSnapshot,
            double requestedVolume)
        {
            Connection = connection;
            Flow = flow;
            SourceId = sourceId;
            DestinationId = destinationId;
            FiniteSource = finiteSource;
            FiniteDestination = finiteDestination;
            SourceSnapshot = sourceSnapshot;
            DestinationSnapshot = destinationSnapshot;
            RequestedVolume = requestedVolume;
        }

        public FloodConnection Connection { get; }

        public FloodFlowResult Flow { get; }

        public FluidBoundaryId SourceId { get; }

        public FluidBoundaryId DestinationId { get; }

        public FloodVolume FiniteSource { get; }

        public FloodVolume FiniteDestination { get; }

        public FluidBoundarySnapshot SourceSnapshot { get; }

        public FluidBoundarySnapshot DestinationSnapshot { get; }

        public double RequestedVolume { get; }

        public bool HasTransfer => RequestedVolume > 0d;

        public bool SourceHasFiniteSupply => SourceSnapshot.HasFiniteSupply;

        public bool DestinationHasFiniteCapacity =>
            DestinationSnapshot.HasFiniteCapacity;
    }
}
