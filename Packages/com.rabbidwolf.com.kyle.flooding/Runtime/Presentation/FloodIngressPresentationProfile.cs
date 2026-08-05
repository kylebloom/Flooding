using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Shared presentation settings for local ingress jet, spread, splash, and
    /// convergence. Never mutates flood simulation.
    /// </summary>
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

        [Header("Lifecycle / Spread Size")]

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

        [SerializeField]
        [Tooltip("Meters to offset local patches along the floor normal to reduce Z-fighting with floor geometry.")]
        [Min(0f)]
        private float floorOffsetMeters = DefaultFloorOffsetMeters;

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
        [Tooltip("Maps normalized 0–1 flow strength to jet / stream visual scale.")]
        private AnimationCurve flowToStreamScale = AnimationCurve.Linear(0f, 0.15f, 1f, 1f);

        [SerializeField]
        [Tooltip("Maps normalized 0–1 flow strength to a multiplier on Local Spread Speed.")]
        private AnimationCurve flowToSpreadSpeed = AnimationCurve.Linear(0f, 0.35f, 1f, 1f);

        [SerializeField]
        [Tooltip("Maps normalized 0–1 flow strength to splash/particle intensity.")]
        private AnimationCurve flowToSplashStrength = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Jet")]

        [SerializeField]
        [Tooltip("Initial jet speed in meters per second at full flow-driven stream scale.")]
        [Min(0f)]
        private float jetInitialSpeed = 4.5f;

        [SerializeField]
        [Tooltip("Ballistic jet lifetime in seconds at full stream scale. Longer lifetime produces a longer arc.")]
        [Min(0.05f)]
        private float jetLifetimeSeconds = 0.55f;

        [SerializeField]
        [Tooltip("Jet source width in meters at full stream scale.")]
        [Min(0.005f)]
        private float jetWidthMeters = 0.14f;

        [SerializeField]
        [Tooltip("End radius as a fraction of source width (0 = needle tip, 1 = no taper).")]
        [Range(0f, 1f)]
        private float jetTaper = 0.35f;

        [SerializeField]
        [Tooltip("Multiplier on ActiveGravity / Physics.gravity applied to the presentation-only ballistic curve.")]
        [Min(0f)]
        private float jetGravityInfluence = 1f;

        [SerializeField]
        [Tooltip("Shader turbulence / distortion strength at full stream scale.")]
        [Min(0f)]
        private float jetTurbulence = 0.35f;

        [SerializeField]
        [Tooltip("UV scroll speed along the jet in cycles per second at full stream scale.")]
        [Min(0f)]
        private float jetUvFlowSpeed = 2.5f;

        [Header("Directional Spread")]

        [SerializeField]
        [Tooltip("Extra major-axis elongation relative to minor axis while Growing (0 = round, 1 = strongly elongated).")]
        [Range(0f, 2f)]
        private float directionalStretch = 0.85f;

        [SerializeField]
        [Tooltip("How quickly directional stretch relaxes toward round during Settling/Converging (units per second).")]
        [Min(0f)]
        private float directionalRelaxation = 0.55f;

        [SerializeField]
        [Tooltip("Shader edge-noise spatial scale for irregular patch boundaries.")]
        [Min(0.01f)]
        private float edgeNoiseScale = 2.4f;

        [SerializeField]
        [Tooltip("Shader edge-noise amplitude. Higher values break the circular silhouette more.")]
        [Min(0f)]
        private float edgeNoiseStrength = 0.35f;

        [SerializeField]
        [Tooltip("Shader soft edge width in normalized radial space.")]
        [Range(0.01f, 0.5f)]
        private float edgeSoftness = 0.18f;

        [SerializeField]
        [Tooltip("Shader ripple amplitude for shallow local water motion.")]
        [Min(0f)]
        private float rippleStrength = 0.12f;

        [SerializeField]
        [Tooltip("Shader ripple animation speed in cycles per second.")]
        [Min(0f)]
        private float rippleSpeed = 1.4f;

        [Header("Splash")]

        [SerializeField]
        [Tooltip("Multiplier on particle emission rate at full splash strength.")]
        [Min(0f)]
        private float splashEmissionMultiplier = 1f;

        [SerializeField]
        [Tooltip("Splash droplet speed scale at full splash strength.")]
        [Min(0f)]
        private float splashDropletSpeed = 2.2f;

        [SerializeField]
        [Tooltip("Splash droplet size scale at full splash strength.")]
        [Min(0f)]
        private float splashDropletSize = 1f;

        [Header("Foam")]

        [SerializeField]
        [Tooltip("Foam tint applied near irregular patch edges and impact cues.")]
        private Color foamColor = new(0.9f, 0.95f, 1f, 1f);

        [SerializeField]
        [Tooltip("Shader foam intensity near irregular patch edges and impact cues.")]
        [Range(0f, 1f)]
        private float foamStrength = 0.45f;

        [SerializeField]
        [Tooltip("Normalized radial width of the foam rim on local patches.")]
        [Range(0.01f, 0.5f)]
        private float foamEdgeWidth = 0.12f;

        [SerializeField]
        [Tooltip("Spatial scale of foam breakup noise along the patch rim.")]
        [Min(0.01f)]
        private float foamNoiseScale = 4.5f;

        [SerializeField]
        [Tooltip("Scroll speed of foam noise along the patch rim in cycles per second.")]
        [Min(0f)]
        private float foamScrollSpeed = 0.65f;

        [SerializeField]
        [Tooltip("Normalized splash strength below which spray-mist emission stays off.")]
        [Range(0f, 1f)]
        private float sprayMistThreshold = 0.35f;

        [SerializeField]
        [Tooltip("Normalized splash strength below which foam-burst emission stays minimal.")]
        [Range(0f, 1f)]
        private float foamBurstThreshold = 0.2f;

        public float LocalSpreadSpeed
        {
            get => localSpreadSpeed;
            set => localSpreadSpeed = Mathf.Max(0f, value);
        }

        public float MaximumLocalRadius
        {
            get => maximumLocalRadius;
            set => maximumLocalRadius = Mathf.Max(0.01f, value);
        }

        public float InitialPoolDepth
        {
            get => initialPoolDepth;
            set => initialPoolDepth = Mathf.Max(0f, value);
        }

        public float SettlingDurationSeconds
        {
            get => settlingDurationSeconds;
            set => settlingDurationSeconds = Mathf.Max(0f, value);
        }

        public float ConvergenceDurationSeconds
        {
            get => convergenceDurationSeconds;
            set => convergenceDurationSeconds = Mathf.Max(0.01f, value);
        }

        public float MinimumFlowRate
        {
            get => minimumFlowRate;
            set => minimumFlowRate = Mathf.Max(0f, value);
        }

        public int MaximumSimultaneousPatches
        {
            get => maximumSimultaneousPatches;
            set => maximumSimultaneousPatches = Mathf.Max(1, value);
        }

        public float LowFlowThreshold
        {
            get => lowFlowThreshold;
            set => lowFlowThreshold = Mathf.Max(0f, value);
        }

        public float HighFlowThreshold
        {
            get => highFlowThreshold;
            set => highFlowThreshold = Mathf.Max(lowFlowThreshold, value);
        }

        public AnimationCurve FlowToStreamScale
        {
            get => flowToStreamScale;
            set => flowToStreamScale = value ?? AnimationCurve.Linear(0f, 0.15f, 1f, 1f);
        }

        public AnimationCurve FlowToSpreadSpeed
        {
            get => flowToSpreadSpeed;
            set => flowToSpreadSpeed = value ?? AnimationCurve.Linear(0f, 0.35f, 1f, 1f);
        }

        public AnimationCurve FlowToSplashStrength
        {
            get => flowToSplashStrength;
            set => flowToSplashStrength = value ?? AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }

        public float FloorOffsetMeters
        {
            get => floorOffsetMeters;
            set => floorOffsetMeters = Mathf.Max(0f, value);
        }

        public float JetInitialSpeed
        {
            get => jetInitialSpeed;
            set => jetInitialSpeed = Mathf.Max(0f, value);
        }

        public float JetLifetimeSeconds
        {
            get => jetLifetimeSeconds;
            set => jetLifetimeSeconds = Mathf.Max(0.05f, value);
        }

        public float JetWidthMeters
        {
            get => jetWidthMeters;
            set => jetWidthMeters = Mathf.Max(0.005f, value);
        }

        public float JetTaper
        {
            get => jetTaper;
            set => jetTaper = Mathf.Clamp01(value);
        }

        public float JetGravityInfluence
        {
            get => jetGravityInfluence;
            set => jetGravityInfluence = Mathf.Max(0f, value);
        }

        public float JetTurbulence
        {
            get => jetTurbulence;
            set => jetTurbulence = Mathf.Max(0f, value);
        }

        public float JetUvFlowSpeed
        {
            get => jetUvFlowSpeed;
            set => jetUvFlowSpeed = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Compatibility alias for approximate jet path length
        /// (<see cref="JetInitialSpeed"/> × <see cref="JetLifetimeSeconds"/>).
        /// </summary>
        public float StreamLengthMeters
        {
            get => jetInitialSpeed * jetLifetimeSeconds;
            set
            {
                var length = Mathf.Max(0f, value);
                if (jetInitialSpeed <= 0.01f)
                    jetInitialSpeed = 4.5f;
                jetLifetimeSeconds = Mathf.Max(0.05f, length / jetInitialSpeed);
            }
        }

        /// <summary>
        /// Compatibility alias for <see cref="JetWidthMeters"/>.
        /// </summary>
        public float StreamWidthMeters
        {
            get => jetWidthMeters;
            set => jetWidthMeters = Mathf.Max(0.005f, value);
        }

        public float DirectionalStretch
        {
            get => directionalStretch;
            set => directionalStretch = Mathf.Clamp(value, 0f, 2f);
        }

        public float DirectionalRelaxation
        {
            get => directionalRelaxation;
            set => directionalRelaxation = Mathf.Max(0f, value);
        }

        public float EdgeNoiseScale
        {
            get => edgeNoiseScale;
            set => edgeNoiseScale = Mathf.Max(0.01f, value);
        }

        public float EdgeNoiseStrength
        {
            get => edgeNoiseStrength;
            set => edgeNoiseStrength = Mathf.Max(0f, value);
        }

        public float EdgeSoftness
        {
            get => edgeSoftness;
            set => edgeSoftness = Mathf.Clamp(value, 0.01f, 0.5f);
        }

        public float RippleStrength
        {
            get => rippleStrength;
            set => rippleStrength = Mathf.Max(0f, value);
        }

        public float RippleSpeed
        {
            get => rippleSpeed;
            set => rippleSpeed = Mathf.Max(0f, value);
        }

        public float SplashEmissionMultiplier
        {
            get => splashEmissionMultiplier;
            set => splashEmissionMultiplier = Mathf.Max(0f, value);
        }

        public float SplashDropletSpeed
        {
            get => splashDropletSpeed;
            set => splashDropletSpeed = Mathf.Max(0f, value);
        }

        public float SplashDropletSize
        {
            get => splashDropletSize;
            set => splashDropletSize = Mathf.Max(0f, value);
        }

        public Color FoamColor
        {
            get => foamColor;
            set => foamColor = value;
        }

        public float FoamStrength
        {
            get => foamStrength;
            set => foamStrength = Mathf.Clamp01(value);
        }

        public float FoamEdgeWidth
        {
            get => foamEdgeWidth;
            set => foamEdgeWidth = Mathf.Clamp(value, 0.01f, 0.5f);
        }

        public float FoamNoiseScale
        {
            get => foamNoiseScale;
            set => foamNoiseScale = Mathf.Max(0.01f, value);
        }

        public float FoamScrollSpeed
        {
            get => foamScrollSpeed;
            set => foamScrollSpeed = Mathf.Max(0f, value);
        }

        public float SprayMistThreshold
        {
            get => sprayMistThreshold;
            set => sprayMistThreshold = Mathf.Clamp01(value);
        }

        public float FoamBurstThreshold
        {
            get => foamBurstThreshold;
            set => foamBurstThreshold = Mathf.Clamp01(value);
        }

        public float EvaluateNormalizedStrength(float flowRateCubicMetersPerSecond)
        {
            return FloodPresentationUtility.FlowIntensity(
                flowRateCubicMetersPerSecond,
                lowFlowThreshold,
                highFlowThreshold);
        }

        public float EvaluateStreamScale(float flowRateCubicMetersPerSecond)
        {
            return EvaluateCurve(
                flowToStreamScale,
                EvaluateNormalizedStrength(flowRateCubicMetersPerSecond),
                1f);
        }

        public float EvaluateSpreadSpeedMultiplier(float flowRateCubicMetersPerSecond)
        {
            return EvaluateCurve(
                flowToSpreadSpeed,
                EvaluateNormalizedStrength(flowRateCubicMetersPerSecond),
                1f);
        }

        public float EvaluateSplashStrength(float flowRateCubicMetersPerSecond)
        {
            return EvaluateCurve(
                flowToSplashStrength,
                EvaluateNormalizedStrength(flowRateCubicMetersPerSecond),
                0f);
        }

        private void OnValidate()
        {
            localSpreadSpeed = SanitizeNonNegative(localSpreadSpeed, DefaultLocalSpreadSpeed);
            maximumLocalRadius = Mathf.Max(
                0.01f,
                SanitizeNonNegative(maximumLocalRadius, DefaultMaximumLocalRadius));
            initialPoolDepth = SanitizeNonNegative(initialPoolDepth, DefaultInitialPoolDepth);
            settlingDurationSeconds = SanitizeNonNegative(
                settlingDurationSeconds,
                DefaultSettlingDurationSeconds);
            convergenceDurationSeconds = Mathf.Max(
                0.01f,
                SanitizeNonNegative(
                    convergenceDurationSeconds,
                    DefaultConvergenceDurationSeconds));
            minimumFlowRate = SanitizeNonNegative(minimumFlowRate, DefaultMinimumFlowRate);
            maximumSimultaneousPatches = Mathf.Max(1, maximumSimultaneousPatches);
            lowFlowThreshold = SanitizeNonNegative(lowFlowThreshold, 0.1f);
            highFlowThreshold = Mathf.Max(
                lowFlowThreshold,
                SanitizeNonNegative(highFlowThreshold, 2f));
            floorOffsetMeters = SanitizeNonNegative(floorOffsetMeters, DefaultFloorOffsetMeters);

            jetInitialSpeed = SanitizeNonNegative(jetInitialSpeed, 4.5f);
            jetLifetimeSeconds = Mathf.Max(0.05f, SanitizeNonNegative(jetLifetimeSeconds, 0.55f));
            jetWidthMeters = Mathf.Max(0.005f, SanitizeNonNegative(jetWidthMeters, 0.14f));
            jetTaper = Mathf.Clamp01(jetTaper);
            jetGravityInfluence = SanitizeNonNegative(jetGravityInfluence, 1f);
            jetTurbulence = SanitizeNonNegative(jetTurbulence, 0.35f);
            jetUvFlowSpeed = SanitizeNonNegative(jetUvFlowSpeed, 2.5f);

            directionalStretch = Mathf.Clamp(directionalStretch, 0f, 2f);
            directionalRelaxation = SanitizeNonNegative(directionalRelaxation, 0.55f);
            edgeNoiseScale = Mathf.Max(0.01f, SanitizeNonNegative(edgeNoiseScale, 2.4f));
            edgeNoiseStrength = SanitizeNonNegative(edgeNoiseStrength, 0.35f);
            edgeSoftness = Mathf.Clamp(edgeSoftness, 0.01f, 0.5f);
            rippleStrength = SanitizeNonNegative(rippleStrength, 0.12f);
            rippleSpeed = SanitizeNonNegative(rippleSpeed, 1.4f);

            splashEmissionMultiplier = SanitizeNonNegative(splashEmissionMultiplier, 1f);
            splashDropletSpeed = SanitizeNonNegative(splashDropletSpeed, 2.2f);
            splashDropletSize = SanitizeNonNegative(splashDropletSize, 1f);
            foamStrength = Mathf.Clamp01(foamStrength);
            foamEdgeWidth = Mathf.Clamp(foamEdgeWidth, 0.01f, 0.5f);
            foamNoiseScale = Mathf.Max(0.01f, SanitizeNonNegative(foamNoiseScale, 4.5f));
            foamScrollSpeed = SanitizeNonNegative(foamScrollSpeed, 0.65f);
            sprayMistThreshold = Mathf.Clamp01(sprayMistThreshold);
            foamBurstThreshold = Mathf.Clamp01(foamBurstThreshold);

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
