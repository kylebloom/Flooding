using UnityEngine;

namespace Kyle.Flooding.URP
{
    /// <summary>
    /// Per-camera presentation bridge that supplies tracker/profile state and a
    /// smoothed effect blend to <see cref="FloodUnderwaterRendererFeature"/>.
    /// </summary>
    /// <remarks>
    /// Does not mutate flood simulation. Attach beside
    /// <see cref="FloodCameraTracker"/> on the target camera.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("Flooding/URP/Flood Underwater Camera Effect")]
    [DefaultExecutionOrder(110)]
    public sealed class FloodUnderwaterCameraEffect : MonoBehaviour
    {
        private const float MinimumWaterVolume = 1e-6f;

        [Header("Sources")]

        [SerializeField]
        [Tooltip("Flood camera tracker that supplies volume, underwater, and surface-plane state. Leave empty to use a FloodCameraTracker on this GameObject.")]
        private FloodCameraTracker tracker;

        [SerializeField]
        [Tooltip("Shared underwater presentation profile. Required for tint/fog/distortion settings.")]
        private FloodUnderwaterProfile profile;

        [Header("Update")]

        [SerializeField]
        [Tooltip("When enabled, EffectBlend is updated from LateUpdate using the profile transition duration.")]
        private bool updateAutomatically = true;

        /// <summary>
        /// Gets or sets the flood camera tracker used for effect state.
        /// </summary>
        public FloodCameraTracker Tracker
        {
            get => tracker;
            set => tracker = value;
        }

        /// <summary>
        /// Gets or sets the underwater presentation profile.
        /// </summary>
        public FloodUnderwaterProfile Profile
        {
            get => profile;
            set => profile = value;
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
        /// Gets the smoothed 0–1 global underwater effect blend.
        /// </summary>
        public float EffectBlend { get; private set; }

        /// <summary>
        /// Gets whether the effect currently has enough state to render.
        /// </summary>
        public bool CanRender =>
            isActiveAndEnabled
            && tracker != null
            && profile != null
            && tracker.ActiveVolume != null
            && EffectBlend > 0.001f;

        private void Awake()
        {
            ResolveTracker();
        }

        private void OnEnable()
        {
            ResolveTracker();
            Refresh(0f);
        }

        private void LateUpdate()
        {
            if (updateAutomatically)
                Refresh(Time.deltaTime);
        }

        /// <summary>
        /// Updates the smoothed effect blend from tracker state.
        /// </summary>
        /// <param name="deltaTime">
        /// Elapsed seconds used for transition smoothing. Use zero to snap.
        /// </param>
        public void Refresh(float deltaTime)
        {
            ResolveTracker();

            var target = EvaluateTargetBlend();
            var duration = profile != null
                ? Mathf.Max(0f, profile.TransitionDurationSeconds)
                : 0f;

            if (duration <= 0f || deltaTime <= 0f)
            {
                EffectBlend = target;
                return;
            }

            EffectBlend = Mathf.MoveTowards(
                EffectBlend,
                target,
                deltaTime / duration);
        }

        private float EvaluateTargetBlend()
        {
            if (tracker == null || profile == null)
                return 0f;

            var volume = tracker.ActiveVolume;
            if (volume == null)
                return 0f;

            if (volume.CurrentVolume <= MinimumWaterVolume)
                return 0f;

            // Inside a flooded compartment: enable the pass so the waterline
            // can cross the view even while the camera is still dry.
            if (tracker.IsInsideFloodVolume)
                return 1f;

            // Latched underwater outside containment should still clear via
            // tracker hysteresis; keep a residual blend only while underwater.
            return tracker.IsUnderwater ? 1f : 0f;
        }

        private void ResolveTracker()
        {
            if (tracker == null)
                tracker = GetComponent<FloodCameraTracker>();
        }
    }
}
