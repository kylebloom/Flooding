using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Shared presentation settings for local ingress spread and convergence.
    /// </summary>
    /// <remarks>
    /// Contains no runtime state and never mutates flood simulation. Sampling
    /// of solver flow does not require this asset; only
    /// <see cref="FloodIngressPresentationState"/> and visual consumers use it.
    /// </remarks>
    [CreateAssetMenu(
        fileName = "FloodIngressPresentationProfile",
        menuName = "Flooding/Flood Ingress Presentation Profile")]
    public sealed class FloodIngressPresentationProfile : ScriptableObject
    {
        public const int DefaultMaximumSimultaneousPatches = 8;
        public const float DefaultLocalSpreadSpeed = 0.75f;
        public const float DefaultMaximumLocalRadius = 3.5f;
        public const float DefaultInitialPoolDepth = 0.04f;
        public const float DefaultSettlingDurationSeconds = 1f;
        public const float DefaultConvergenceDurationSeconds = 4f;
        public const float DefaultMinimumFlowRate = 0.01f;
        public const float DefaultFloorOffsetMeters = 0.01f;

        [Header("Spread")]

        [SerializeField]
        [Tooltip("Base radial expansion speed of a local ingress patch in meters per second at full flow-driven spread strength.")]
        [Min(0f)]
        private float localSpreadSpeed = DefaultLocalSpreadSpeed;

        [SerializeField]
        [Tooltip("Maximum visual radius of a local ingress patch in meters.")]
        [Min(0.01f)]
        private float maximumLocalRadius = DefaultMaximumLocalRadius;

        [SerializeField]
        [Tooltip("Initial shallow visual depth of a new local pool in meters. Presentation only.")]
        [Min(0f)]
        private float initialPoolDepth = DefaultInitialPoolDepth;

        [Header("Lifecycle")]

        [SerializeField]
        [Tooltip("Seconds a stopped patch remains in Settling before Converging begins.")]
        [Min(0f)]
        private float settlingDurationSeconds = DefaultSettlingDurationSeconds;

        [SerializeField]
        [Tooltip("Seconds spent fading local presentation into the bulk surface during Converging.")]
        [Min(0.01f)]
        private float convergenceDurationSeconds = DefaultConvergenceDurationSeconds;

        [SerializeField]
        [Tooltip("Absolute inflow rate in cubic meters per second below which a sample is ignored for Growing.")]
        [Min(0f)]
        private float minimumFlowRate = DefaultMinimumFlowRate;

        [SerializeField]
        [Tooltip("Maximum simultaneous local patches tracked per presenter. One patch is owned per provider.")]
        [Min(1)]
        private int maximumSimultaneousPatches = DefaultMaximumSimultaneousPatches;

        [Header("Flow Response")]

        [SerializeField]
        [Tooltip("Absolute flow in cubic meters per second at or below which normalized strength stays in the low band.")]
        [Min(0f)]
        private float lowFlowThreshold = 0.1f;

        [SerializeField]
        [Tooltip("Absolute flow in cubic meters per second at or above which normalized strength saturates.")]
        [Min(0f)]
        private float highFlowThreshold = 2f;

        [SerializeField]
        [Tooltip("Maps normalized 0–1 flow strength to stream visual scale.")]
        private AnimationCurve flowToStreamScale = AnimationCurve.Linear(0f, 0.15f, 1f, 1f);

        [SerializeField]
        [Tooltip("Maps normalized 0–1 flow strength to a multiplier on Local Spread Speed.")]
        private AnimationCurve flowToSpreadSpeed = AnimationCurve.Linear(0f, 0.35f, 1f, 1f);

        [SerializeField]
        [Tooltip("Maps normalized 0–1 flow strength to splash/particle intensity.")]
        private AnimationCurve flowToSplashStrength = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Floor / Stream")]

        [SerializeField]
        [Tooltip("Meters to offset local patches along the floor normal to reduce Z-fighting with floor geometry.")]
        [Min(0f)]
        private float floorOffsetMeters = DefaultFloorOffsetMeters;

        [SerializeField]
        [Tooltip("Default stream visual length in meters at full stream scale.")]
        [Min(0f)]
        private float streamLengthMeters = 1.25f;

        [SerializeField]
        [Tooltip("Default stream visual width in meters at full stream scale.")]
        [Min(0f)]
        private float streamWidthMeters = 0.12f;

        /// <summary>
        /// Gets or sets base radial expansion speed in meters per second.
        /// </summary>
        public float LocalSpreadSpeed
        {
            get => localSpreadSpeed;
            set => localSpreadSpeed = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Gets or sets the maximum visual patch radius in meters.
        /// </summary>
        public float MaximumLocalRadius
        {
            get => maximumLocalRadius;
            set => maximumLocalRadius = Mathf.Max(0.01f, value);
        }

        /// <summary>
        /// Gets or sets the initial shallow visual depth in meters.
        /// </summary>
        public float InitialPoolDepth
        {
            get => initialPoolDepth;
            set => initialPoolDepth = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Gets or sets settling duration in seconds after inflow stops.
        /// </summary>
        public float SettlingDurationSeconds
        {
            get => settlingDurationSeconds;
            set => settlingDurationSeconds = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Gets or sets converging / handoff duration in seconds.
        /// </summary>
        public float ConvergenceDurationSeconds
        {
            get => convergenceDurationSeconds;
            set => convergenceDurationSeconds = Mathf.Max(0.01f, value);
        }

        /// <summary>
        /// Gets or sets the minimum inflow rate that creates or sustains Growing.
        /// </summary>
        public float MinimumFlowRate
        {
            get => minimumFlowRate;
            set => minimumFlowRate = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Gets or sets the maximum simultaneous provider-owned patches.
        /// </summary>
        public int MaximumSimultaneousPatches
        {
            get => maximumSimultaneousPatches;
            set => maximumSimultaneousPatches = Mathf.Max(1, value);
        }

        /// <summary>
        /// Gets or sets the low-band absolute flow threshold.
        /// </summary>
        public float LowFlowThreshold
        {
            get => lowFlowThreshold;
            set => lowFlowThreshold = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Gets or sets the high-band absolute flow threshold.
        /// </summary>
        public float HighFlowThreshold
        {
            get => highFlowThreshold;
            set => highFlowThreshold = Mathf.Max(lowFlowThreshold, value);
        }

        /// <summary>
        /// Gets or sets the normalized-strength → stream scale curve.
        /// </summary>
        public AnimationCurve FlowToStreamScale
        {
            get => flowToStreamScale;
            set => flowToStreamScale = value ?? AnimationCurve.Linear(0f, 0.15f, 1f, 1f);
        }

        /// <summary>
        /// Gets or sets the normalized-strength → spread-speed multiplier curve.
        /// </summary>
        public AnimationCurve FlowToSpreadSpeed
        {
            get => flowToSpreadSpeed;
            set => flowToSpreadSpeed = value ?? AnimationCurve.Linear(0f, 0.35f, 1f, 1f);
        }

        /// <summary>
        /// Gets or sets the normalized-strength → splash strength curve.
        /// </summary>
        public AnimationCurve FlowToSplashStrength
        {
            get => flowToSplashStrength;
            set => flowToSplashStrength = value ?? AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }

        /// <summary>
        /// Gets or sets floor offset along the presentation normal in meters.
        /// </summary>
        public float FloorOffsetMeters
        {
            get => floorOffsetMeters;
            set => floorOffsetMeters = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Gets or sets default stream length in meters.
        /// </summary>
        public float StreamLengthMeters
        {
            get => streamLengthMeters;
            set => streamLengthMeters = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Gets or sets default stream width in meters.
        /// </summary>
        public float StreamWidthMeters
        {
            get => streamWidthMeters;
            set => streamWidthMeters = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Maps an absolute flow rate onto a 0–1 normalized strength.
        /// </summary>
        public float EvaluateNormalizedStrength(float flowRateCubicMetersPerSecond)
        {
            return FloodPresentationUtility.FlowIntensity(
                flowRateCubicMetersPerSecond,
                lowFlowThreshold,
                highFlowThreshold);
        }

        /// <summary>
        /// Evaluates stream scale from absolute flow rate.
        /// </summary>
        public float EvaluateStreamScale(float flowRateCubicMetersPerSecond)
        {
            return EvaluateCurve(
                flowToStreamScale,
                EvaluateNormalizedStrength(flowRateCubicMetersPerSecond),
                1f);
        }

        /// <summary>
        /// Evaluates spread-speed multiplier from absolute flow rate.
        /// </summary>
        public float EvaluateSpreadSpeedMultiplier(float flowRateCubicMetersPerSecond)
        {
            return EvaluateCurve(
                flowToSpreadSpeed,
                EvaluateNormalizedStrength(flowRateCubicMetersPerSecond),
                1f);
        }

        /// <summary>
        /// Evaluates splash strength from absolute flow rate.
        /// </summary>
        public float EvaluateSplashStrength(float flowRateCubicMetersPerSecond)
        {
            return EvaluateCurve(
                flowToSplashStrength,
                EvaluateNormalizedStrength(flowRateCubicMetersPerSecond),
                0f);
        }

        private void OnValidate()
        {
            localSpreadSpeed = SanitizeNonNegative(
                localSpreadSpeed,
                DefaultLocalSpreadSpeed);
            maximumLocalRadius = Mathf.Max(
                0.01f,
                SanitizeNonNegative(maximumLocalRadius, DefaultMaximumLocalRadius));
            initialPoolDepth = SanitizeNonNegative(
                initialPoolDepth,
                DefaultInitialPoolDepth);
            settlingDurationSeconds = SanitizeNonNegative(
                settlingDurationSeconds,
                DefaultSettlingDurationSeconds);
            convergenceDurationSeconds = Mathf.Max(
                0.01f,
                SanitizeNonNegative(
                    convergenceDurationSeconds,
                    DefaultConvergenceDurationSeconds));
            minimumFlowRate = SanitizeNonNegative(
                minimumFlowRate,
                DefaultMinimumFlowRate);
            maximumSimultaneousPatches = Mathf.Max(1, maximumSimultaneousPatches);
            lowFlowThreshold = SanitizeNonNegative(lowFlowThreshold, 0.1f);
            highFlowThreshold = Mathf.Max(
                lowFlowThreshold,
                SanitizeNonNegative(highFlowThreshold, 2f));
            floorOffsetMeters = SanitizeNonNegative(
                floorOffsetMeters,
                DefaultFloorOffsetMeters);
            streamLengthMeters = SanitizeNonNegative(streamLengthMeters, 1.25f);
            streamWidthMeters = SanitizeNonNegative(streamWidthMeters, 0.12f);

            flowToStreamScale ??= AnimationCurve.Linear(0f, 0.15f, 1f, 1f);
            flowToSpreadSpeed ??= AnimationCurve.Linear(0f, 0.35f, 1f, 1f);
            flowToSplashStrength ??= AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }

        private static float EvaluateCurve(
            AnimationCurve curve,
            float normalized,
            float fallback)
        {
            if (curve == null || curve.length == 0)
                return fallback;

            return Mathf.Max(0f, curve.Evaluate(Mathf.Clamp01(normalized)));
        }

        private static float SanitizeNonNegative(float value, float fallback)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
                return fallback;

            return value;
        }
    }
}
