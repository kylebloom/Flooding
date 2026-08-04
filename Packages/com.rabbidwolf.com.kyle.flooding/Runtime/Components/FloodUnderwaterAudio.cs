using UnityEngine;
using UnityEngine.Audio;

namespace Kyle.Flooding
{
    /// <summary>
    /// Presentation-only underwater audio driven by
    /// <see cref="FloodCameraTracker"/> through exposed
    /// <see cref="AudioMixer"/> parameters.
    /// </summary>
    /// <remarks>
    /// Does not require rendering support and never mutates flood simulation.
    /// Expose the named parameters on the mixer (right-click parameter →
    /// Expose) before Play Mode.
    /// </remarks>
    [DisallowMultipleComponent]
    [AddComponentMenu("Flooding/Flood Underwater Audio")]
    [DefaultExecutionOrder(120)]
    public sealed class FloodUnderwaterAudio : MonoBehaviour
    {
        [Header("Source")]

        [SerializeField]
        [Tooltip("Flood camera tracker that supplies underwater state. Leave empty to find one on this GameObject or the Main Camera.")]
        private FloodCameraTracker tracker;

        [Header("Mixer")]

        [SerializeField]
        [Tooltip("AudioMixer that owns the exposed low-pass and volume parameters.")]
        private AudioMixer audioMixer;

        [SerializeField]
        [Tooltip("Exposed mixer parameter name for low-pass cutoff frequency in Hertz.")]
        private string lowPassParameter = "FloodLowPassCutoff";

        [SerializeField]
        [Tooltip("Optional exposed mixer parameter name for volume in decibels. Leave empty to skip volume changes.")]
        private string volumeParameter = "FloodUnderwaterVolume";

        [Header("Low Pass")]

        [SerializeField]
        [Tooltip("Low-pass cutoff in Hertz while the camera is above water.")]
        [Min(10f)]
        private float normalLowPassCutoffHz = 22000f;

        [SerializeField]
        [Tooltip("Low-pass cutoff in Hertz while the camera is underwater.")]
        [Min(10f)]
        private float underwaterLowPassCutoffHz = 700f;

        [Header("Volume")]

        [SerializeField]
        [Tooltip("Mixer volume in decibels while the camera is above water.")]
        private float normalVolumeDb = 0f;

        [SerializeField]
        [Tooltip("Mixer volume in decibels while the camera is underwater.")]
        private float underwaterVolumeDb = -4f;

        [Header("Transition")]

        [SerializeField]
        [Tooltip("Seconds used to smooth mixer parameters between normal and underwater values. Uses deltaTime-correct MoveTowards.")]
        [Min(0f)]
        private float transitionDurationSeconds = 0.25f;

        [SerializeField]
        [Tooltip("When enabled, Refresh runs automatically from LateUpdate.")]
        private bool updateAutomatically = true;

        private float currentLowPassCutoffHz;
        private float currentVolumeDb;
        private bool hasAppliedValues;
        private bool warnedMissingMixer;

        /// <summary>
        /// Gets or sets the flood camera tracker that drives this audio.
        /// </summary>
        public FloodCameraTracker Tracker
        {
            get => tracker;
            set => tracker = value;
        }

        /// <summary>
        /// Gets or sets the AudioMixer that receives parameter updates.
        /// </summary>
        public AudioMixer AudioMixer
        {
            get => audioMixer;
            set => audioMixer = value;
        }

        /// <summary>
        /// Gets or sets the exposed low-pass cutoff parameter name.
        /// </summary>
        public string LowPassParameter
        {
            get => lowPassParameter;
            set => lowPassParameter = value;
        }

        /// <summary>
        /// Gets or sets the optional exposed volume parameter name.
        /// </summary>
        public string VolumeParameter
        {
            get => volumeParameter;
            set => volumeParameter = value;
        }

        /// <summary>
        /// Gets or sets the above-water low-pass cutoff in Hertz.
        /// </summary>
        public float NormalLowPassCutoffHz
        {
            get => normalLowPassCutoffHz;
            set => normalLowPassCutoffHz = Mathf.Max(10f, value);
        }

        /// <summary>
        /// Gets or sets the underwater low-pass cutoff in Hertz.
        /// </summary>
        public float UnderwaterLowPassCutoffHz
        {
            get => underwaterLowPassCutoffHz;
            set => underwaterLowPassCutoffHz = Mathf.Max(10f, value);
        }

        /// <summary>
        /// Gets or sets the above-water mixer volume in decibels.
        /// </summary>
        public float NormalVolumeDb
        {
            get => normalVolumeDb;
            set => normalVolumeDb = value;
        }

        /// <summary>
        /// Gets or sets the underwater mixer volume in decibels.
        /// </summary>
        public float UnderwaterVolumeDb
        {
            get => underwaterVolumeDb;
            set => underwaterVolumeDb = value;
        }

        /// <summary>
        /// Gets or sets the transition duration in seconds.
        /// </summary>
        public float TransitionDurationSeconds
        {
            get => transitionDurationSeconds;
            set => transitionDurationSeconds = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Gets the latest smoothed low-pass cutoff in Hertz.
        /// </summary>
        public float CurrentLowPassCutoffHz => currentLowPassCutoffHz;

        /// <summary>
        /// Gets the latest smoothed mixer volume in decibels.
        /// </summary>
        public float CurrentVolumeDb => currentVolumeDb;

        /// <summary>
        /// Gets the latest 0–1 underwater audio blend.
        /// </summary>
        public float CurrentUnderwaterBlend { get; private set; }

        private void Awake()
        {
            ResolveTracker();
            InitializeCurrentValues(tracker != null && tracker.IsUnderwater);
        }

        private void OnEnable()
        {
            ResolveTracker();
            if (!hasAppliedValues)
                InitializeCurrentValues(tracker != null && tracker.IsUnderwater);

            Refresh(0f);
        }

        private void OnDisable()
        {
            CurrentUnderwaterBlend = 0f;
            currentLowPassCutoffHz = normalLowPassCutoffHz;
            currentVolumeDb = normalVolumeDb;
            if (audioMixer != null)
                ApplyMixerParameters();
        }

        private void LateUpdate()
        {
            if (updateAutomatically)
                Refresh(Time.deltaTime);
        }

        private void OnValidate()
        {
            normalLowPassCutoffHz = Mathf.Max(
                10f,
                Sanitize(normalLowPassCutoffHz, 22000f));
            underwaterLowPassCutoffHz = Mathf.Max(
                10f,
                Sanitize(underwaterLowPassCutoffHz, 700f));
            normalVolumeDb = Sanitize(normalVolumeDb, 0f);
            underwaterVolumeDb = Sanitize(underwaterVolumeDb, -4f);
            transitionDurationSeconds = Mathf.Max(
                0f,
                Sanitize(transitionDurationSeconds, 0.25f));
        }

        /// <summary>
        /// Updates mixer parameters from the tracker underwater state.
        /// </summary>
        /// <param name="deltaTime">
        /// Elapsed seconds for smoothing. Use zero to snap to targets.
        /// </param>
        public void Refresh(float deltaTime)
        {
            ResolveTracker();

            if (!isActiveAndEnabled)
            {
                CurrentUnderwaterBlend = 0f;
                return;
            }

            var targetBlend = tracker != null && tracker.IsUnderwater ? 1f : 0f;
            CurrentUnderwaterBlend = Smooth(
                CurrentUnderwaterBlend,
                targetBlend,
                transitionDurationSeconds,
                deltaTime);

            currentLowPassCutoffHz = Mathf.Lerp(
                normalLowPassCutoffHz,
                underwaterLowPassCutoffHz,
                CurrentUnderwaterBlend);
            currentVolumeDb = Mathf.Lerp(
                normalVolumeDb,
                underwaterVolumeDb,
                CurrentUnderwaterBlend);

            if (audioMixer != null)
                ApplyMixerParameters();

            hasAppliedValues = true;
        }

        private void InitializeCurrentValues(bool underwater)
        {
            CurrentUnderwaterBlend = underwater ? 1f : 0f;
            currentLowPassCutoffHz = underwater
                ? underwaterLowPassCutoffHz
                : normalLowPassCutoffHz;
            currentVolumeDb = underwater
                ? underwaterVolumeDb
                : normalVolumeDb;
            hasAppliedValues = true;
        }

        private void ApplyMixerParameters()
        {
            if (!string.IsNullOrEmpty(lowPassParameter)
                && !audioMixer.SetFloat(lowPassParameter, currentLowPassCutoffHz)
                && !warnedMissingMixer)
            {
                Debug.LogWarning(
                    $"FloodUnderwaterAudio on '{name}' could not set AudioMixer parameter '{lowPassParameter}'. Expose it on the mixer.",
                    this);
                warnedMissingMixer = true;
            }

            if (!string.IsNullOrEmpty(volumeParameter)
                && !audioMixer.SetFloat(volumeParameter, currentVolumeDb)
                && !warnedMissingMixer)
            {
                Debug.LogWarning(
                    $"FloodUnderwaterAudio on '{name}' could not set AudioMixer parameter '{volumeParameter}'. Expose it on the mixer or clear the Volume Parameter field.",
                    this);
                warnedMissingMixer = true;
            }
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

        private static float Smooth(
            float current,
            float target,
            float durationSeconds,
            float deltaTime)
        {
            if (durationSeconds <= 0f || deltaTime <= 0f)
                return target;

            return Mathf.MoveTowards(current, target, deltaTime / durationSeconds);
        }

        private static float Sanitize(float value, float fallback)
        {
            return float.IsNaN(value) || float.IsInfinity(value)
                ? fallback
                : value;
        }
    }
}
