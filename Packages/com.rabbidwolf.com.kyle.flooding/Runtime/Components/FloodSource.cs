using System;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Requests configured volume injection during fixed simulation ticks.
    /// </summary>
    /// <remarks>
    /// Use <see cref="FloodSource"/> for gameplay-controlled or scripted
    /// injection such as a broken pipe, sprinkler, or debug faucet. It does
    /// not model pressure equilibrium. For ocean, lake, or reservoir exchange
    /// that depends on opening depth and can reverse, use
    /// <see cref="ExternalFluidBoundary"/> with a <see cref="FloodConnection"/>.
    /// </remarks>
    public sealed class FloodSource : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Manager that evaluates this source. If unassigned, the target or nearest parent manager is used.")]
        private FloodSimulationManager simulationManager;

        [SerializeField]
        [Tooltip("Flood volume that receives water from this configured source. Prefer External Fluid Body + FloodConnection when inflow should depend on pressure head.")]
        private FloodVolume target;

        [SerializeField]
        [Tooltip("Configured injection rate in cubic meters per second. This is not pressure-driven.")]
        [Min(0f)]
        private float flowRate = 1f;

        [SerializeField]
        [Tooltip("Whether this source currently requests water during simulation ticks.")]
        private bool active = true;

        /// <summary>
        /// Gets or sets the manager that evaluates this source.
        /// </summary>
        public FloodSimulationManager SimulationManager
        {
            get => simulationManager;
            set => SetSimulationManager(value);
        }

        /// <summary>
        /// Gets or sets the volume that receives this source's inflow.
        /// </summary>
        public FloodVolume Target
        {
            get => target;
            set
            {
                target = value;

                if (target != null && target.SimulationManager != null)
                    SetSimulationManager(target.SimulationManager);
                else
                    ResolveManagerRegistration();
            }
        }

        /// <summary>
        /// Gets or sets the requested flow rate in cubic meters per second.
        /// </summary>
        public float FlowRate
        {
            get => flowRate;
            set
            {
                EnsureFinite(value, nameof(value));
                flowRate = Mathf.Max(0f, value);
            }
        }

        /// <summary>
        /// Gets or sets whether this source requests inflow.
        /// </summary>
        public bool IsActive
        {
            get => active;
            set => active = value;
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
            if (float.IsNaN(flowRate) || float.IsInfinity(flowRate))
                flowRate = 0f;
            else
                flowRate = Mathf.Max(0f, flowRate);

            if (simulationManager == null)
            {
                simulationManager =
                    target != null
                        ? target.SimulationManager
                            ?? GetComponentInParent<FloodSimulationManager>()
                        : GetComponentInParent<FloodSimulationManager>();
            }
        }

        internal bool TryGetRequestedInflow(
            FloodSimulationManager manager,
            double deltaTime,
            out FloodVolume targetVolume,
            out double requestedVolume)
        {
            targetVolume = target;
            requestedVolume = 0d;

            if (
                !isActiveAndEnabled
                || !active
                || target == null
                || simulationManager != manager
                || target.SimulationManager != manager
                || flowRate <= 0f)
            {
                return false;
            }

            requestedVolume = flowRate * deltaTime;

            return
                !double.IsNaN(requestedVolume)
                && !double.IsInfinity(requestedVolume)
                && requestedVolume > 0d;
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
            {
                simulationManager =
                    target != null && target.SimulationManager != null
                        ? target.SimulationManager
                        : GetComponentInParent<FloodSimulationManager>();
            }

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

        private static void EnsureFinite(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
