using System;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Requests configured volume removal during fixed simulation ticks.
    /// </summary>
    /// <remarks>
    /// Use <see cref="FloodSink"/> for gameplay-controlled pumps, bilge systems,
    /// or drains that extract water from a finite compartment into nowhere
    /// (water leaves the simulation). Requests are manager-mediated and share
    /// finite supply with connection outflows. <see cref="FlowRate"/> is the
    /// configured maximum requested removal rate, not a guaranteed applied rate;
    /// read <see cref="CurrentFlowRate"/> for the last-tick applied rate.
    /// Intake submergence, power, and damage belong in gameplay code that sets
    /// <see cref="IsActive"/> / <see cref="FlowRate"/>.
    /// </remarks>
    [DisallowMultipleComponent]
    [AddComponentMenu("Flooding/Flood Sink")]
    public sealed class FloodSink : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Manager that evaluates this sink. If unassigned, the target or nearest parent manager is used.")]
        private FloodSimulationManager simulationManager;

        [SerializeField]
        [Tooltip("Finite flood volume that this sink removes water from.")]
        private FloodVolume target;

        [SerializeField]
        [Tooltip("Configured maximum requested removal rate in cubic meters per second. Actual removal may be lower after supply reconciliation.")]
        [Min(0f)]
        private float flowRate = 1f;

        [SerializeField]
        [Tooltip("Whether this sink currently requests removal during simulation ticks.")]
        private bool active = true;

        /// <summary>
        /// Gets or sets the manager that evaluates this sink.
        /// </summary>
        public FloodSimulationManager SimulationManager
        {
            get => simulationManager;
            set => SetSimulationManager(value);
        }

        /// <summary>
        /// Gets or sets the finite volume this sink removes water from.
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
        /// Gets or sets the configured maximum requested removal rate in cubic
        /// meters per second.
        /// </summary>
        /// <remarks>
        /// This is not a guaranteed applied rate. Competing connection outflows
        /// and limited compartment volume may reduce the accepted amount.
        /// </remarks>
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
        /// Gets or sets whether this sink requests removal.
        /// </summary>
        public bool IsActive
        {
            get => active;
            set => active = value;
        }

        /// <summary>
        /// Gets the configured removal rate that would be requested this frame
        /// when active, in cubic meters per second.
        /// </summary>
        public float RequestedFlowRate =>
            isActiveAndEnabled && active && target != null && flowRate > 0f
                ? flowRate
                : 0f;

        /// <summary>
        /// Gets the supply-constrained removal rate applied during the latest
        /// tick, in cubic meters per second.
        /// </summary>
        public float CurrentFlowRate { get; private set; }

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
            ResetTickState();
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

        internal bool TryGetRequestedOutflow(
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

        internal void ApplyTickResult(double appliedRemovalRateCubicMetersPerSecond)
        {
            CurrentFlowRate = (float)Math.Max(
                0d,
                appliedRemovalRateCubicMetersPerSecond);
        }

        internal void ResetTickState()
        {
            CurrentFlowRate = 0f;
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
