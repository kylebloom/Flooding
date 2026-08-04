using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Shared presentation settings for underwater camera effects.
    /// </summary>
    /// <remarks>
    /// Contains no runtime state and is safe to share across cameras and scenes.
    /// Consumers (URP passes, audio, UI) read these values; the profile never
    /// mutates flood simulation.
    /// </remarks>
    [CreateAssetMenu(
        fileName = "FloodUnderwaterProfile",
        menuName = "Flooding/Flood Underwater Profile")]
    public sealed class FloodUnderwaterProfile : ScriptableObject
    {
        /// <summary>
        /// Default submersion depth at which underwater effects reach full
        /// strength (meters).
        /// </summary>
        public const float DefaultFullEffectDepthMeters = 2f;

        /// <summary>
        /// Default blend duration when approaching or leaving the waterline
        /// (seconds).
        /// </summary>
        public const float DefaultTransitionDurationSeconds = 0.25f;

        [Header("Tint")]

        [SerializeField]
        [Tooltip("Underwater tint near the surface. RGBA; alpha contributes to effect opacity when consumers use it.")]
        private Color shallowTintColor = new(0.45f, 0.70f, 0.78f, 0.35f);

        [SerializeField]
        [Tooltip("Underwater tint at Full Effect Depth and deeper. RGBA; alpha contributes to effect opacity when consumers use it.")]
        private Color deepTintColor = new(0.08f, 0.22f, 0.38f, 0.72f);

        [Header("Depth")]

        [SerializeField]
        [Tooltip("Submersion depth in meters at which tint, fog, and related strengths reach their configured maxima. Must be greater than zero.")]
        [Min(0.01f)]
        private float fullEffectDepthMeters = DefaultFullEffectDepthMeters;

        [Header("Fog")]

        [SerializeField]
        [Tooltip("Base underwater fog density scale used by presentation consumers. Dimensionless; higher values thicken fog faster with depth.")]
        [Min(0f)]
        private float fogDensity = 0.12f;

        [SerializeField]
        [Tooltip("Maximum fog strength clamped to 0–1 after depth scaling.")]
        [Range(0f, 1f)]
        private float maximumFogStrength = 0.8f;

        [Header("Color Grading")]

        [SerializeField]
        [Tooltip("Saturation multiplier while underwater. 1 = unchanged, below 1 desaturates.")]
        [Range(0f, 2f)]
        private float saturation = 0.85f;

        [SerializeField]
        [Tooltip("Contrast multiplier while underwater. 1 = unchanged.")]
        [Range(0f, 2f)]
        private float contrast = 1.05f;

        [Header("Distortion")]

        [SerializeField]
        [Tooltip("UV distortion amplitude for underwater presentation. Keep small (about 0–0.05) for subtle motion.")]
        [Min(0f)]
        private float distortionStrength = 0.008f;

        [SerializeField]
        [Tooltip("UV distortion animation speed in cycles per second.")]
        [Min(0f)]
        private float distortionSpeed = 0.35f;

        [Header("Transitions")]

        [SerializeField]
        [Tooltip("Seconds used to smooth enter/exit and near-surface blend. Consumers should apply deltaTime-correct smoothing.")]
        [Min(0f)]
        private float transitionDurationSeconds = DefaultTransitionDurationSeconds;

        /// <summary>
        /// Gets or sets the underwater tint near the surface.
        /// </summary>
        public Color ShallowTintColor
        {
            get => shallowTintColor;
            set => shallowTintColor = value;
        }

        /// <summary>
        /// Gets or sets the underwater tint at full effect depth and deeper.
        /// </summary>
        public Color DeepTintColor
        {
            get => deepTintColor;
            set => deepTintColor = value;
        }

        /// <summary>
        /// Gets or sets the submersion depth in meters at which effects reach
        /// full strength.
        /// </summary>
        public float FullEffectDepthMeters
        {
            get => fullEffectDepthMeters;
            set => fullEffectDepthMeters = Mathf.Max(0.01f, value);
        }

        /// <summary>
        /// Gets or sets the base underwater fog density scale.
        /// </summary>
        public float FogDensity
        {
            get => fogDensity;
            set => fogDensity = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Gets or sets the maximum fog strength in the 0–1 range.
        /// </summary>
        public float MaximumFogStrength
        {
            get => maximumFogStrength;
            set => maximumFogStrength = Mathf.Clamp01(value);
        }

        /// <summary>
        /// Gets or sets the underwater saturation multiplier.
        /// </summary>
        public float Saturation
        {
            get => saturation;
            set => saturation = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Gets or sets the underwater contrast multiplier.
        /// </summary>
        public float Contrast
        {
            get => contrast;
            set => contrast = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Gets or sets UV distortion amplitude.
        /// </summary>
        public float DistortionStrength
        {
            get => distortionStrength;
            set => distortionStrength = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Gets or sets UV distortion speed in cycles per second.
        /// </summary>
        public float DistortionSpeed
        {
            get => distortionSpeed;
            set => distortionSpeed = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Gets or sets the blend duration in seconds for enter/exit smoothing.
        /// </summary>
        public float TransitionDurationSeconds
        {
            get => transitionDurationSeconds;
            set => transitionDurationSeconds = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Maps submersion depth onto a 0–1 depth strength using
        /// <see cref="FullEffectDepthMeters"/>.
        /// </summary>
        /// <param name="submersionDepthMeters">
        /// Depth below the surface in meters. Negative values are treated as
        /// zero.
        /// </param>
        public float EvaluateDepthStrength(float submersionDepthMeters)
        {
            if (float.IsNaN(submersionDepthMeters)
                || float.IsInfinity(submersionDepthMeters)
                || submersionDepthMeters <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(submersionDepthMeters / fullEffectDepthMeters);
        }

        /// <summary>
        /// Interpolates shallow and deep tint colors by depth strength.
        /// </summary>
        /// <param name="submersionDepthMeters">
        /// Depth below the surface in meters.
        /// </param>
        public Color EvaluateTintColor(float submersionDepthMeters)
        {
            return Color.Lerp(
                shallowTintColor,
                deepTintColor,
                EvaluateDepthStrength(submersionDepthMeters));
        }

        /// <summary>
        /// Evaluates fog strength from depth, shaped by <see cref="FogDensity"/>
        /// and clamped by <see cref="MaximumFogStrength"/>.
        /// </summary>
        /// <param name="submersionDepthMeters">
        /// Depth below the surface in meters.
        /// </param>
        public float EvaluateFogStrength(float submersionDepthMeters)
        {
            var depthStrength = EvaluateDepthStrength(submersionDepthMeters);
            if (depthStrength <= 0f || fogDensity <= 0f || maximumFogStrength <= 0f)
                return 0f;

            // Density controls how quickly fog approaches the configured maximum
            // within the full-effect depth ramp.
            var shaped = 1f - Mathf.Exp(-fogDensity * 6f * depthStrength);
            return maximumFogStrength * Mathf.Clamp01(shaped);
        }

        private void OnValidate()
        {
            if (float.IsNaN(fullEffectDepthMeters)
                || float.IsInfinity(fullEffectDepthMeters)
                || fullEffectDepthMeters < 0.01f)
            {
                fullEffectDepthMeters = DefaultFullEffectDepthMeters;
            }

            fogDensity = SanitizeNonNegative(fogDensity, 0.12f);
            maximumFogStrength = Mathf.Clamp01(
                SanitizeNonNegative(maximumFogStrength, 0.8f));
            saturation = SanitizeNonNegative(saturation, 0.85f);
            contrast = SanitizeNonNegative(contrast, 1.05f);
            distortionStrength = SanitizeNonNegative(distortionStrength, 0.008f);
            distortionSpeed = SanitizeNonNegative(distortionSpeed, 0.35f);
            transitionDurationSeconds = SanitizeNonNegative(
                transitionDurationSeconds,
                DefaultTransitionDurationSeconds);
        }

        private static float SanitizeNonNegative(float value, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
                return fallback;

            return value;
        }
    }
}
