using System;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Infinite external fluid boundary such as an ocean, lake, or reservoir.
    /// </summary>
    /// <remarks>
    /// Author the waterline with this component's Transform, or an optional
    /// surface Transform override: position is a point on the surface and up is
    /// the surface normal. For an open-air body, the surface normal should
    /// oppose gravity.
    /// </remarks>
    [AddComponentMenu("Flooding/External Fluid Body")]
    [DisallowMultipleComponent]
    public sealed class ExternalFluidBoundary : MonoBehaviour, IFluidBoundary
    {
        private const float MinimumDensity = 0.01f;

        [Header("Simulation")]

        [SerializeField]
        [Tooltip("Manager that captures this boundary. If unassigned, the nearest parent manager is used.")]
        private FloodSimulationManager simulationManager;

        [Header("Surface")]

        [SerializeField]
        [Tooltip("Optional Transform that defines the waterline. When unassigned, this GameObject's Transform is used. Position is a point on the surface; up is the surface normal.")]
        private Transform surfaceTransform;

        [Header("Fluid")]

        [SerializeField]
        [Tooltip("Fluid density in kilograms per cubic meter. Must match connected FloodVolume densities within tolerance. Fresh water is approximately 1000 kg/m³.")]
        [Min(MinimumDensity)]
        private float density = 1000f;

        [SerializeField]
        [Tooltip("Whether this boundary participates in connection evaluation.")]
        private bool boundaryEnabled = true;

        [Header("Debug")]

        [SerializeField]
        [Tooltip("Draws the authored surface plane when this GameObject is selected.")]
        private bool drawSurfaceGizmo = true;

        /// <summary>
        /// Gets or sets the manager that captures this boundary.
        /// </summary>
        public FloodSimulationManager SimulationManager
        {
            get => simulationManager;
            set => SetSimulationManager(value);
        }

        /// <summary>
        /// Gets or sets the optional surface Transform override.
        /// </summary>
        public Transform SurfaceTransform
        {
            get => surfaceTransform;
            set => surfaceTransform = value;
        }

        /// <summary>
        /// Gets or sets fluid density in kilograms per cubic meter.
        /// </summary>
        public float Density
        {
            get => density;
            set
            {
                EnsureValidDensity(value);
                density = value;
            }
        }

        /// <summary>
        /// Gets or sets whether this boundary participates in evaluation.
        /// </summary>
        public bool BoundaryEnabled
        {
            get => boundaryEnabled;
            set => boundaryEnabled = value;
        }

        /// <summary>
        /// Gets or sets whether the selected-object surface gizmo is drawn.
        /// </summary>
        public bool DrawSurfaceGizmo
        {
            get => drawSurfaceGizmo;
            set => drawSurfaceGizmo = value;
        }

        /// <inheritdoc />
        public FluidBoundaryId BoundaryId => FluidBoundaryId.FromObject(this);

        /// <inheritdoc />
        public bool IsBoundaryEnabled =>
            isActiveAndEnabled && boundaryEnabled;

        /// <summary>
        /// Gets the world-space surface plane from the authored Transform.
        /// </summary>
        public Plane SurfacePlane
        {
            get
            {
                var source = ResolveSurfaceSource();
                var normal = source.up;

                if (normal.sqrMagnitude <= FloodGeometryTolerances.PlaneNormal)
                    normal = Vector3.up;
                else
                    normal.Normalize();

                return new Plane(normal, source.position);
            }
        }

        private void Awake()
        {
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
            if (float.IsNaN(density) || float.IsInfinity(density) || density < MinimumDensity)
                density = 1000f;

            if (simulationManager == null)
                simulationManager = GetComponentInParent<FloodSimulationManager>();
        }

        /// <inheritdoc />
        public FluidBoundarySnapshot CaptureBoundarySnapshot()
        {
            return new FluidBoundarySnapshot(
                BoundaryId,
                simulationManager,
                SurfacePlane,
                density,
                hasFiniteSupply: false,
                availableVolume: 0d,
                hasFiniteCapacity: false,
                remainingCapacity: 0d,
                acceptsCommits: false,
                isEnabled: IsBoundaryEnabled);
        }

        /// <summary>
        /// Configures fluid density used for matching-density validation.
        /// </summary>
        public void ConfigureDensity(float kilogramsPerCubicMeter)
        {
            Density = kilogramsPerCubicMeter;
        }

        internal void UseManagerIfUnset(FloodSimulationManager manager)
        {
            if (simulationManager == null)
                SetSimulationManager(manager);
            else if (simulationManager == manager && isActiveAndEnabled)
                simulationManager.Register(this);
        }

        private Transform ResolveSurfaceSource()
        {
            return surfaceTransform != null ? surfaceTransform : transform;
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

        private static void EnsureValidDensity(float value)
        {
            if (
                float.IsNaN(value)
                || float.IsInfinity(value)
                || value < MinimumDensity)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    value,
                    $"Density must be finite and at least {MinimumDensity} kg/m³.");
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawSurfaceGizmo)
                return;

            var source = ResolveSurfaceSource();
            var plane = SurfacePlane;
            var center = source.position;
            var normal = plane.normal;
            var tangent = Vector3.Cross(normal, source.right);

            if (tangent.sqrMagnitude < 0.0001f)
                tangent = Vector3.Cross(normal, Vector3.forward);

            tangent.Normalize();
            var bitangent = Vector3.Cross(normal, tangent).normalized;
            const float extent = 2.5f;

            Gizmos.color = new Color(0.15f, 0.55f, 1f, 0.85f);
            var corner0 = center + ((tangent + bitangent) * extent);
            var corner1 = center + ((tangent - bitangent) * extent);
            var corner2 = center + ((-tangent - bitangent) * extent);
            var corner3 = center + ((-tangent + bitangent) * extent);
            Gizmos.DrawLine(corner0, corner1);
            Gizmos.DrawLine(corner1, corner2);
            Gizmos.DrawLine(corner2, corner3);
            Gizmos.DrawLine(corner3, corner0);
            Gizmos.DrawLine(center, center + (normal * 1.25f));
        }
    }
}
