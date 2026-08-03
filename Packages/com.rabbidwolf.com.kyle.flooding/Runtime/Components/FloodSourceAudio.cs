using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Presentation-only audio driven by a configured <see cref="FloodSource"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    [AddComponentMenu("Flooding/Flood Source Audio")]
    public sealed class FloodSourceAudio : MonoBehaviour
    {
        [Header("Source")]

        [SerializeField]
        [Tooltip("Configured FloodSource whose active state and flow rate drive this audio.")]
        private FloodSource source;

        [Header("Audio")]

        [SerializeField]
        [Tooltip("AudioSource that plays the injection sound. Spatial blend should usually be 3D.")]
        private AudioSource audioSource;

        [SerializeField]
        [Tooltip("Optional clip assigned when missing on the AudioSource.")]
        private AudioClip flowClip;

        [Header("Response")]

        [SerializeField]
        [Tooltip("Configured source flow in cubic meters per second treated as full intensity.")]
        [Min(0.01f)]
        private float fullFlowRate = 1f;

        [SerializeField]
        [Tooltip("AudioSource volume at full configured flow.")]
        [Range(0f, 1f)]
        private float volumeAtFullFlow = 0.7f;

        [SerializeField]
        [Tooltip("AudioSource pitch at low configured flow.")]
        [Min(0.1f)]
        private float pitchAtLowFlow = 0.9f;

        [SerializeField]
        [Tooltip("AudioSource pitch at full configured flow.")]
        [Min(0.1f)]
        private float pitchAtFullFlow = 1.2f;

        /// <summary>
        /// Gets or sets the configured source that drives this audio.
        /// </summary>
        public FloodSource Source
        {
            get => source;
            set => source = value;
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
            fullFlowRate = Mathf.Max(0.01f, SanitizeNonNegative(fullFlowRate, 1f));
            volumeAtFullFlow = Mathf.Clamp01(volumeAtFullFlow);
            pitchAtLowFlow = Mathf.Max(0.1f, SanitizeNonNegative(pitchAtLowFlow, 0.9f));
            pitchAtFullFlow = Mathf.Max(0.1f, SanitizeNonNegative(pitchAtFullFlow, 1.2f));
            ResolveReferences();
        }

        /// <summary>
        /// Immediately refreshes audio from the current source settings.
        /// </summary>
        public void Refresh()
        {
            if (audioSource == null || source == null || !isActiveAndEnabled)
            {
                CurrentIntensity = 0f;
                ApplySilent();
                return;
            }

            EnsureClip();

            if (!source.IsActive || source.FlowRate <= 0f || audioSource.clip == null)
            {
                CurrentIntensity = 0f;
                ApplySilent();
                return;
            }

            CurrentIntensity = Mathf.Clamp01(source.FlowRate / fullFlowRate);
            audioSource.volume = volumeAtFullFlow * CurrentIntensity;
            audioSource.pitch = Mathf.Lerp(
                pitchAtLowFlow,
                pitchAtFullFlow,
                CurrentIntensity);

            if (!audioSource.isPlaying)
                audioSource.Play();
        }

        private void ResolveReferences()
        {
            if (source == null)
                source = GetComponent<FloodSource>();

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
