using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Presentation-only ambient audio driven by a <see cref="FloodVolume"/>'s
    /// fill state.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    [AddComponentMenu("Flooding/Flood Volume Audio")]
    public sealed class FloodVolumeAudio : MonoBehaviour
    {
        [Header("Source")]

        [SerializeField]
        [Tooltip("Flood volume whose fill percentage drives this ambient audio.")]
        private FloodVolume volume;

        [Header("Audio")]

        [SerializeField]
        [Tooltip("AudioSource that plays compartment ambience. Spatial blend should usually be 3D.")]
        private AudioSource audioSource;

        [SerializeField]
        [Tooltip("Optional clip assigned when missing on the AudioSource.")]
        private AudioClip ambienceClip;

        [Header("Response")]

        [SerializeField]
        [Tooltip("Fill percentage at or below which ambience stays silent.")]
        [Range(0f, 1f)]
        private float silentBelowFill = 0.02f;

        [SerializeField]
        [Tooltip("Fill percentage at or above which ambience reaches full volume.")]
        [Range(0f, 1f)]
        private float fullAtFill = 0.85f;

        [SerializeField]
        [Tooltip("AudioSource volume at full fill intensity.")]
        [Range(0f, 1f)]
        private float volumeAtFullFill = 0.55f;

        [SerializeField]
        [Tooltip("AudioSource pitch at low fill.")]
        [Min(0.1f)]
        private float pitchAtLowFill = 0.95f;

        [SerializeField]
        [Tooltip("AudioSource pitch when the compartment is nearly full.")]
        [Min(0.1f)]
        private float pitchAtFullFill = 0.75f;

        /// <summary>
        /// Gets or sets the flood volume that drives this audio.
        /// </summary>
        public FloodVolume Volume
        {
            get => volume;
            set => volume = value;
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
            silentBelowFill = Mathf.Clamp01(silentBelowFill);
            fullAtFill = Mathf.Max(silentBelowFill, Mathf.Clamp01(fullAtFill));
            volumeAtFullFill = Mathf.Clamp01(volumeAtFullFill);
            pitchAtLowFill = Mathf.Max(0.1f, SanitizeNonNegative(pitchAtLowFill, 0.95f));
            pitchAtFullFill = Mathf.Max(0.1f, SanitizeNonNegative(pitchAtFullFill, 0.75f));
            ResolveReferences();
        }

        /// <summary>
        /// Immediately refreshes ambience from the current volume state.
        /// </summary>
        public void Refresh()
        {
            if (audioSource == null || volume == null || !isActiveAndEnabled)
            {
                CurrentIntensity = 0f;
                ApplySilent();
                return;
            }

            EnsureClip();

            var fill = FloodPresentationUtility.FillIntensity(volume.FillPercentage);
            if (fill <= silentBelowFill || audioSource.clip == null)
            {
                CurrentIntensity = 0f;
                ApplySilent();
                return;
            }

            var span = Mathf.Max(0.0001f, fullAtFill - silentBelowFill);
            CurrentIntensity = Mathf.Clamp01((fill - silentBelowFill) / span);
            audioSource.volume = volumeAtFullFill * CurrentIntensity;
            audioSource.pitch = Mathf.Lerp(
                pitchAtLowFill,
                pitchAtFullFill,
                CurrentIntensity);

            if (!audioSource.isPlaying)
                audioSource.Play();
        }

        private void ResolveReferences()
        {
            if (volume == null)
                volume = GetComponent<FloodVolume>();

            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();
        }

        private void EnsureClip()
        {
            if (audioSource != null && audioSource.clip == null && ambienceClip != null)
                audioSource.clip = ambienceClip;
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
