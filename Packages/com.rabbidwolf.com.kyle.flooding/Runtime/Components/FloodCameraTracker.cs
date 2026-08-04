using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kyle.Flooding
{
    /// <summary>
    /// Presentation-only tracker that relates a viewpoint to nearby
    /// <see cref="FloodVolume"/> state. Does not render, mutate simulation, or
    /// require a render pipeline.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Flooding/Flood Camera Tracker")]
    [DefaultExecutionOrder(100)]
    public sealed class FloodCameraTracker : MonoBehaviour
    {
        private const float ManagerRetryIntervalSeconds = 0.5f;

        [Header("Viewpoint")]

        [SerializeField]
        [Tooltip("World-space viewpoint used for flood queries. Leave empty to use this transform, or the Main Camera when this component is not on a camera.")]
        private Transform viewpoint;

        [Header("Volume Selection")]

        [SerializeField]
        [Tooltip("Explicit: track one assigned FloodVolume. Auto Discover Registered: sticky selection among FloodSimulationManager.RegisteredVolumes.")]
        private FloodCameraVolumeSelectionMode volumeSelectionMode =
            FloodCameraVolumeSelectionMode.AutoDiscoverRegistered;

        [SerializeField]
        [Tooltip("Flood volume tracked when Volume Selection Mode is Explicit.")]
        private FloodVolume explicitVolume;

        [SerializeField]
        [Tooltip("Manager whose RegisteredVolumes list is used for Auto Discover Registered selection. Leave empty to resolve a parent manager or FindAnyObjectByType, retrying periodically until one appears (supports late-loaded simulation scenes).")]
        private FloodSimulationManager manager;

        [Header("Underwater Hysteresis")]

        [SerializeField]
        [Tooltip("Enter underwater when currently dry and SurfaceSignedDistanceMeters is at or below this value (meters). Negative means the viewpoint must cross slightly below the surface. Default -0.02.")]
        private float enterWaterThresholdMeters =
            FloodCameraUnderwaterHysteresis.DefaultEnterWaterThresholdMeters;

        [SerializeField]
        [Tooltip("Exit underwater when currently underwater and SurfaceSignedDistanceMeters is at or above this value (meters). Positive means the viewpoint must cross slightly above the surface. Default +0.02.")]
        private float exitWaterThresholdMeters =
            FloodCameraUnderwaterHysteresis.DefaultExitWaterThresholdMeters;

        [Header("Update")]

        [SerializeField]
        [Tooltip("When enabled, Refresh runs automatically from LateUpdate.")]
        private bool updateAutomatically = true;

        private float nextManagerResolveUnscaledTime;

        /// <summary>
        /// Gets or sets the world-space viewpoint used for flood queries.
        /// </summary>
        public Transform Viewpoint
        {
            get => viewpoint;
            set => viewpoint = value;
        }

        /// <summary>
        /// Gets or sets how the active flood volume is chosen.
        /// </summary>
        public FloodCameraVolumeSelectionMode VolumeSelectionMode
        {
            get => volumeSelectionMode;
            set => volumeSelectionMode = value;
        }

        /// <summary>
        /// Gets or sets the volume used when
        /// <see cref="VolumeSelectionMode"/> is
        /// <see cref="FloodCameraVolumeSelectionMode.Explicit"/>.
        /// </summary>
        public FloodVolume ExplicitVolume
        {
            get => explicitVolume;
            set => explicitVolume = value;
        }

        /// <summary>
        /// Gets or sets the manager used for automatic volume discovery.
        /// </summary>
        public FloodSimulationManager Manager
        {
            get => manager;
            set
            {
                manager = value;
                nextManagerResolveUnscaledTime = 0f;
            }
        }

        /// <summary>
        /// Gets or sets the signed-distance threshold for entering water
        /// (meters). Enforces <c>enter &lt;= exit</c> at runtime.
        /// </summary>
        public float EnterWaterThresholdMeters
        {
            get => enterWaterThresholdMeters;
            set
            {
                enterWaterThresholdMeters = SanitizeThreshold(
                    value,
                    FloodCameraUnderwaterHysteresis.DefaultEnterWaterThresholdMeters);
                EnforceEnterNotAboveExit();
            }
        }

        /// <summary>
        /// Gets or sets the signed-distance threshold for exiting water
        /// (meters). Enforces <c>enter &lt;= exit</c> at runtime.
        /// </summary>
        public float ExitWaterThresholdMeters
        {
            get => exitWaterThresholdMeters;
            set
            {
                exitWaterThresholdMeters = SanitizeThreshold(
                    value,
                    FloodCameraUnderwaterHysteresis.DefaultExitWaterThresholdMeters);
                EnforceEnterNotAboveExit();
            }
        }

        /// <summary>
        /// Gets or sets whether <see cref="Refresh"/> runs from LateUpdate.
        /// </summary>
        public bool UpdateAutomatically
        {
            get => updateAutomatically;
            set => updateAutomatically = value;
        }

        /// <summary>
        /// Gets the flood volume currently driving tracker state.
        /// </summary>
        /// <remarks>
        /// In auto-discover mode this is sticky while the volume still contains
        /// the viewpoint, even when the camera is dry. Overlapping volumes are
        /// ambiguous and not physically merged.
        /// </remarks>
        public FloodVolume ActiveVolume { get; private set; }

        /// <summary>
        /// Gets whether the viewpoint lies inside <see cref="ActiveVolume"/>.
        /// </summary>
        public bool IsInsideFloodVolume { get; private set; }

        /// <summary>
        /// Gets whether the viewpoint is latched underwater after hysteresis.
        /// </summary>
        public bool IsUnderwater { get; private set; }

        /// <summary>
        /// Gets the signed distance from the viewpoint to the active volume's
        /// surface plane (meters). Positive above, negative below. Zero when
        /// there is no active volume.
        /// </summary>
        public float SurfaceSignedDistanceMeters { get; private set; }

        /// <summary>
        /// Gets submersion depth from the latest query (meters). Zero when not
        /// submerged or when there is no active volume.
        /// </summary>
        public float SubmersionDepthMeters { get; private set; }

        /// <summary>
        /// Gets the closest point on the active surface plane, or the viewpoint
        /// position when there is no active volume.
        /// </summary>
        public Vector3 SurfacePoint { get; private set; }

        /// <summary>
        /// Gets the active surface normal, or <see cref="Vector3.up"/> when
        /// there is no active volume.
        /// </summary>
        public Vector3 SurfaceNormal { get; private set; }

        /// <summary>
        /// Gets the latest query against <see cref="ActiveVolume"/>.
        /// </summary>
        public FloodQueryResult CurrentQuery { get; private set; }

        /// <summary>
        /// Raised when the viewpoint enters a flood volume.
        /// </summary>
        public event Action<FloodVolume> EnteredFloodVolume;

        /// <summary>
        /// Raised when the viewpoint exits its active flood volume.
        /// </summary>
        public event Action<FloodVolume> ExitedFloodVolume;

        /// <summary>
        /// Raised when hysteresis latches the viewpoint as underwater.
        /// </summary>
        public event Action EnteredWater;

        /// <summary>
        /// Raised when hysteresis clears the underwater latch.
        /// </summary>
        public event Action ExitedWater;

        /// <summary>
        /// Raised when <see cref="ActiveVolume"/> changes, including to null.
        /// </summary>
        public event Action<FloodVolume> ActiveVolumeChanged;

        private void Awake()
        {
            ResolveViewpointIfNeeded();
            nextManagerResolveUnscaledTime = 0f;
            ResolveManagerIfNeeded();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            ResolveViewpointIfNeeded();
            nextManagerResolveUnscaledTime = 0f;
            ResolveManagerIfNeeded();
            Refresh();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void LateUpdate()
        {
            if (updateAutomatically)
                Refresh();
        }

        private void OnValidate()
        {
            enterWaterThresholdMeters = SanitizeThreshold(
                enterWaterThresholdMeters,
                FloodCameraUnderwaterHysteresis.DefaultEnterWaterThresholdMeters);
            exitWaterThresholdMeters = SanitizeThreshold(
                exitWaterThresholdMeters,
                FloodCameraUnderwaterHysteresis.DefaultExitWaterThresholdMeters);
            EnforceEnterNotAboveExit();
        }

        /// <summary>
        /// Recomputes volume selection, query state, and underwater hysteresis.
        /// </summary>
        public void Refresh()
        {
            ResolveViewpointIfNeeded();
            ResolveManagerIfNeeded();

            var samplePoint = ResolveSamplePoint();
            var previousActive = ActiveVolume;
            var previousInside = IsInsideFloodVolume;
            var previousUnderwater = IsUnderwater;

            var nextActive = ResolveActiveVolume(samplePoint);
            var nextQuery = default(FloodQueryResult);
            var nextInside = false;
            var nextSigned = 0f;
            var nextDepth = 0f;
            var nextSurfacePoint = samplePoint;
            var nextSurfaceNormal = Vector3.up;

            if (nextActive != null)
            {
                nextQuery = nextActive.QueryPoint(samplePoint);
                nextInside = nextQuery.IsInsideVolume;
                nextSigned = nextQuery.SurfaceSignedDistanceMeters;
                nextDepth = nextQuery.SubmersionDepthMeters;
                nextSurfacePoint = nextQuery.SurfacePoint;
                nextSurfaceNormal = nextQuery.SurfaceNormal;
            }

            var nextUnderwater = FloodCameraUnderwaterHysteresis.Evaluate(
                previousUnderwater,
                nextInside,
                nextSigned,
                enterWaterThresholdMeters,
                exitWaterThresholdMeters);

            ActiveVolume = nextActive;
            CurrentQuery = nextQuery;
            IsInsideFloodVolume = nextInside;
            SurfaceSignedDistanceMeters = nextSigned;
            SubmersionDepthMeters = nextDepth;
            SurfacePoint = nextSurfacePoint;
            SurfaceNormal = nextSurfaceNormal;
            IsUnderwater = nextUnderwater;

            if (previousActive != nextActive)
                ActiveVolumeChanged?.Invoke(nextActive);

            var exitedVolume = previousInside
                && previousActive != null
                && (!nextInside || previousActive != nextActive);
            if (exitedVolume)
                ExitedFloodVolume?.Invoke(previousActive);

            var enteredVolume = nextInside
                && nextActive != null
                && (!previousInside || previousActive != nextActive);
            if (enteredVolume)
                EnteredFloodVolume?.Invoke(nextActive);

            if (!previousUnderwater && nextUnderwater)
                EnteredWater?.Invoke();
            else if (previousUnderwater && !nextUnderwater)
                ExitedWater?.Invoke();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (manager == null)
                nextManagerResolveUnscaledTime = 0f;
        }

        private FloodVolume ResolveActiveVolume(Vector3 samplePoint)
        {
            if (volumeSelectionMode == FloodCameraVolumeSelectionMode.Explicit)
                return explicitVolume;

            if (manager == null)
                return null;

            return FloodCameraVolumeSelection.SelectActiveVolume(
                ActiveVolume,
                manager.RegisteredVolumes,
                samplePoint);
        }

        private Vector3 ResolveSamplePoint()
        {
            var source = viewpoint != null ? viewpoint : transform;
            return source.position;
        }

        private void ResolveViewpointIfNeeded()
        {
            if (viewpoint != null)
                return;

            if (TryGetComponent<Camera>(out _))
            {
                viewpoint = transform;
                return;
            }

            var mainCamera = Camera.main;
            if (mainCamera != null)
                viewpoint = mainCamera.transform;
        }

        private void ResolveManagerIfNeeded()
        {
            if (manager != null)
                return;

            if (volumeSelectionMode
                != FloodCameraVolumeSelectionMode.AutoDiscoverRegistered)
            {
                return;
            }

            if (Time.unscaledTime < nextManagerResolveUnscaledTime)
                return;

            manager = GetComponentInParent<FloodSimulationManager>();
            if (manager == null)
            {
                manager = UnityEngine.Object.FindAnyObjectByType<FloodSimulationManager>();
            }

            if (manager == null)
            {
                nextManagerResolveUnscaledTime =
                    Time.unscaledTime + ManagerRetryIntervalSeconds;
            }
            else
            {
                nextManagerResolveUnscaledTime = 0f;
            }
        }

        private void EnforceEnterNotAboveExit()
        {
            if (enterWaterThresholdMeters > exitWaterThresholdMeters)
                exitWaterThresholdMeters = enterWaterThresholdMeters;
        }

        private static float SanitizeThreshold(float value, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return fallback;

            return value;
        }
    }
}
