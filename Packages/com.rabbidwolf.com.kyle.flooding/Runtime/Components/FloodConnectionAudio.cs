using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Presentation-only audio driven by a <see cref="FloodConnection"/>'s
    /// measured flow diagnostics.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    [AddComponentMenu("Flooding/Flood Connection Audio")]
    public sealed class FloodConnectionAudio : MonoBehaviour
    {
        [Header("Source")]

        [SerializeField]
        [Tooltip("Flood connection whose applied flow and submerged area drive this audio.")]
        private FloodConnection connection;

        [Header("Audio")]

        [SerializeField]
        [Tooltip("AudioSource that plays the connection flow sound. Spatial blend should usually be 3D.")]
        private AudioSource audioSource;

        [SerializeField]
        [Tooltip("Optional clip assigned when missing on the AudioSource. Leave empty to keep the AudioSource clip.")]
        private AudioClip flowClip;

        [Header("Response")]

        [SerializeField]
        [Tooltip("Absolute applied flow in cubic meters per second at or below which intensity stays in the low band.")]
        [Min(0f)]
        private float lowFlowThreshold = 0.25f;

        [SerializeField]
        [Tooltip("Absolute applied flow in cubic meters per second at or above which intensity saturates.")]
        [Min(0f)]
        private float highFlowThreshold = 2f;

        [SerializeField]
        [Tooltip("AudioSource volume at full flow intensity.")]
        [Range(0f, 1f)]
        private float volumeAtFullFlow = 0.8f;

        [SerializeField]
        [Tooltip("AudioSource pitch at idle/low flow.")]
        [Min(0.1f)]
        private float pitchAtLowFlow = 0.85f;

        [SerializeField]
        [Tooltip("AudioSource pitch at full flow intensity.")]
        [Min(0.1f)]
        private float pitchAtFullFlow = 1.25f;

        /// <summary>
        /// Gets or sets the connection that drives this audio.
        /// </summary>
        public FloodConnection Connection
        {
            get => connection;
            set => connection = value;
        }

        /// <summary>
        /// Gets the latest 0–1 intensity applied to volume and pitch.
        /// </summary>
        public float CurrentIntensity { get; private set; }

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            ApplySilent();
        }

        private void OnDisable()
        {
            ApplySilent();
        }

        private void LateUpdate()
        {
            Refresh();
        }

        private void OnValidate()
        {
            lowFlowThreshold = SanitizeNonNegative(lowFlowThreshold, 0.25f);
            highFlowThreshold = Mathf.Max(
                lowFlowThreshold,
                SanitizeNonNegative(highFlowThreshold, 2f));
            volumeAtFullFlow = Mathf.Clamp01(volumeAtFullFlow);
            pitchAtLowFlow = Mathf.Max(0.1f, SanitizeNonNegative(pitchAtLowFlow, 0.85f));
            pitchAtFullFlow = Mathf.Max(0.1f, SanitizeNonNegative(pitchAtFullFlow, 1.25f));
            ResolveReferences();
        }

        /// <summary>
        /// Immediately refreshes audio from the current connection diagnostics.
        /// </summary>
        public void Refresh()
        {
            if (audioSource == null || connection == null || !isActiveAndEnabled)
            {
                CurrentIntensity = 0f;
                ApplySilent();
                return;
            }

            EnsureClip();

            var rate = System.Math.Abs(connection.CurrentFlowRate);
            var flowing = FloodPresentationUtility.IsFlowing(connection.CurrentFlowRate);
            CurrentIntensity = FloodPresentationUtility.FlowIntensity(
                rate,
                lowFlowThreshold,
                highFlowThreshold);

            if (!flowing || CurrentIntensity <= 0f || audioSource.clip == null)
            {
                ApplySilent();
                return;
            }

            // Larger submerged openings slightly brighten the soundscape.
            var areaBoost = Mathf.Clamp01(
                (float)connection.SubmergedOpeningArea * 0.1f);
            audioSource.volume =
                volumeAtFullFlow
                * Mathf.Clamp01(CurrentIntensity + (areaBoost * 0.15f));
            audioSource.pitch = Mathf.Lerp(
                pitchAtLowFlow,
                pitchAtFullFlow,
                CurrentIntensity);

            if (!audioSource.isPlaying)
                audioSource.Play();
        }

        private void ResolveReferences()
        {
            if (connection == null)
                connection = GetComponent<FloodConnection>();

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        private void EnsureClip()
        {
            if (audioSource != null && audioSource.clip == null && flowClip != null)
                audioSource.clip = flowClip;
        }

        private void ApplySilent()
        {
            if (audioSource == null)
                return;

            audioSource.volume = 0f;

            if (audioSource.isPlaying)
                audioSource.Stop();
        }

        private static float SanitizeNonNegative(float value, float fallback)
        {
            return float.IsNaN(value) || float.IsInfinity(value)
                ? fallback
                : Mathf.Max(0f, value);
        }
    }
}
