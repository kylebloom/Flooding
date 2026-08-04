using System;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Framework-neutral telemetry adapter for a <see cref="FloodVolume"/>.
    /// </summary>
    /// <remarks>
    /// Exposes fill, volume, capacity, and optional connection flow values for
    /// UI bindings without depending on TextMeshPro or uGUI. Presentation only;
    /// never mutates simulation.
    /// </remarks>
    [DisallowMultipleComponent]
    [AddComponentMenu("Flooding/Flood Volume Telemetry")]
    public sealed class FloodVolumeTelemetry : MonoBehaviour
    {
        [Header("Sources")]

        [SerializeField]
        [Tooltip("Flood volume that supplies fill, volume, and capacity telemetry. Leave empty to use a FloodVolume on this GameObject.")]
        private FloodVolume volume;

        [SerializeField]
        [Tooltip("Optional FloodConnection whose public flow rate is reported. Leave empty to skip connection telemetry.")]
        private FloodConnection connection;

        [Header("Update")]

        [SerializeField]
        [Tooltip("When enabled, Refresh runs automatically from LateUpdate.")]
        private bool updateAutomatically = true;

        /// <summary>
        /// Gets or sets the flood volume that supplies telemetry.
        /// </summary>
        public FloodVolume Volume
        {
            get => volume;
            set => volume = value;
        }

        /// <summary>
        /// Gets or sets the optional connection whose flow rate is reported.
        /// </summary>
        public FloodConnection Connection
        {
            get => connection;
            set => connection = value;
        }

        /// <summary>
        /// Gets fill percentage in the 0–1 range.
        /// </summary>
        public float FillPercentage { get; private set; }

        /// <summary>
        /// Gets current water volume in cubic meters.
        /// </summary>
        public float CurrentVolumeCubicMeters { get; private set; }

        /// <summary>
        /// Gets compartment capacity in cubic meters.
        /// </summary>
        public float CapacityCubicMeters { get; private set; }

        /// <summary>
        /// Gets whether the volume is empty.
        /// </summary>
        public bool IsEmpty { get; private set; }

        /// <summary>
        /// Gets whether the volume is full.
        /// </summary>
        public bool IsFull { get; private set; }

        /// <summary>
        /// Gets the optional connection's current signed flow rate in cubic
        /// meters per second, or zero when no connection is assigned.
        /// </summary>
        public float ConnectionFlowRateCubicMetersPerSecond { get; private set; }

        /// <summary>
        /// Gets whether a connection is assigned for flow telemetry.
        /// </summary>
        public bool HasConnection => connection != null;

        /// <summary>
        /// Raised after telemetry values are refreshed.
        /// </summary>
        public event Action ValuesChanged;

        private void Awake()
        {
            ResolveVolume();
        }

        private void OnEnable()
        {
            ResolveVolume();
            Refresh();
        }

        private void LateUpdate()
        {
            if (updateAutomatically)
                Refresh();
        }

        /// <summary>
        /// Reads the latest volume and optional connection diagnostics.
        /// </summary>
        public void Refresh()
        {
            ResolveVolume();

            float nextFill;
            float nextVolume;
            float nextCapacity;
            bool nextEmpty;
            bool nextFull;
            float nextFlow;

            if (volume == null)
            {
                nextFill = 0f;
                nextVolume = 0f;
                nextCapacity = 0f;
                nextEmpty = true;
                nextFull = false;
                nextFlow = 0f;
            }
            else
            {
                var state = volume.CurrentState;
                nextFill = Mathf.Clamp01((float)state.FillPercentage);
                nextVolume = (float)state.Volume;
                nextCapacity = (float)state.Capacity;
                nextEmpty = state.IsEmpty;
                nextFull = state.IsFull;
                nextFlow = connection != null
                    ? (float)connection.CurrentFlowRate
                    : 0f;
            }

            var changed = !Mathf.Approximately(FillPercentage, nextFill)
                || !Mathf.Approximately(CurrentVolumeCubicMeters, nextVolume)
                || !Mathf.Approximately(CapacityCubicMeters, nextCapacity)
                || IsEmpty != nextEmpty
                || IsFull != nextFull
                || !Mathf.Approximately(
                    ConnectionFlowRateCubicMetersPerSecond,
                    nextFlow);

            FillPercentage = nextFill;
            CurrentVolumeCubicMeters = nextVolume;
            CapacityCubicMeters = nextCapacity;
            IsEmpty = nextEmpty;
            IsFull = nextFull;
            ConnectionFlowRateCubicMetersPerSecond = nextFlow;

            if (changed)
                ValuesChanged?.Invoke();
        }

        private void ResolveVolume()
        {
            if (volume == null)
                volume = GetComponent<FloodVolume>();
        }
    }
}
