using System;
using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Framework-neutral telemetry adapter for a
    /// <see cref="FloodCameraTracker"/>.
    /// </summary>
    /// <remarks>
    /// Exposes camera flood depth and underwater state for UI bindings without
    /// depending on TextMeshPro or uGUI. Presentation only; never mutates
    /// simulation.
    /// </remarks>
    [DisallowMultipleComponent]
    [AddComponentMenu("Flooding/Flood Camera Telemetry")]
    [DefaultExecutionOrder(115)]
    public sealed class FloodCameraTelemetry : MonoBehaviour
    {
        [Header("Source")]

        [SerializeField]
        [Tooltip("Flood camera tracker that supplies camera flood telemetry. Leave empty to find one on this GameObject or the Main Camera.")]
        private FloodCameraTracker tracker;

        [Header("Update")]

        [SerializeField]
        [Tooltip("When enabled, Refresh runs automatically from LateUpdate.")]
        private bool updateAutomatically = true;

        /// <summary>
        /// Gets or sets the flood camera tracker that supplies telemetry.
        /// </summary>
        public FloodCameraTracker Tracker
        {
            get => tracker;
            set => tracker = value;
        }

        /// <summary>
        /// Gets whether the viewpoint is inside the active flood volume.
        /// </summary>
        public bool IsInsideFloodVolume { get; private set; }

        /// <summary>
        /// Gets whether the viewpoint is latched underwater.
        /// </summary>
        public bool IsUnderwater { get; private set; }

        /// <summary>
        /// Gets signed surface distance in meters (positive above water).
        /// </summary>
        public float SurfaceSignedDistanceMeters { get; private set; }

        /// <summary>
        /// Gets submersion depth in meters (zero when not submerged).
        /// </summary>
        public float SubmersionDepthMeters { get; private set; }

        /// <summary>
        /// Gets the active flood volume, or null when none is selected.
        /// </summary>
        public FloodVolume ActiveVolume { get; private set; }

        /// <summary>
        /// Raised after telemetry values are refreshed.
        /// </summary>
        public event Action ValuesChanged;

        private void Awake()
        {
            ResolveTracker();
        }

        private void OnEnable()
        {
            ResolveTracker();
            Refresh();
        }

        private void LateUpdate()
        {
            if (updateAutomatically)
                Refresh();
        }

        /// <summary>
        /// Reads the latest tracker presentation state.
        /// </summary>
        public void Refresh()
        {
            ResolveTracker();

            bool nextInside;
            bool nextUnderwater;
            float nextSigned;
            float nextDepth;
            FloodVolume nextVolume;

            if (tracker == null)
            {
                nextInside = false;
                nextUnderwater = false;
                nextSigned = 0f;
                nextDepth = 0f;
                nextVolume = null;
            }
            else
            {
                nextInside = tracker.IsInsideFloodVolume;
                nextUnderwater = tracker.IsUnderwater;
                nextSigned = tracker.SurfaceSignedDistanceMeters;
                nextDepth = tracker.SubmersionDepthMeters;
                nextVolume = tracker.ActiveVolume;
            }

            var changed = IsInsideFloodVolume != nextInside
                || IsUnderwater != nextUnderwater
                || !Mathf.Approximately(SurfaceSignedDistanceMeters, nextSigned)
                || !Mathf.Approximately(SubmersionDepthMeters, nextDepth)
                || ActiveVolume != nextVolume;

            IsInsideFloodVolume = nextInside;
            IsUnderwater = nextUnderwater;
            SurfaceSignedDistanceMeters = nextSigned;
            SubmersionDepthMeters = nextDepth;
            ActiveVolume = nextVolume;

            if (changed)
                ValuesChanged?.Invoke();
        }

        private void ResolveTracker()
        {
            if (tracker != null)
                return;

            tracker = GetComponent<FloodCameraTracker>();
            if (tracker != null)
                return;

            var mainCamera = Camera.main;
            if (mainCamera != null)
                tracker = mainCamera.GetComponent<FloodCameraTracker>();
        }
    }
}
