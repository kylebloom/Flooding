using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Presentation-only visual driven by a <see cref="FloodConnection"/>'s
    /// measured flow diagnostics.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Flooding/Flood Connection Visual")]
    public sealed class FloodConnectionVisual : MonoBehaviour
    {
        [Header("Source")]

        [SerializeField]
        [Tooltip("Flood connection whose applied flow, submerged area, and direction drive this visual.")]
        private FloodConnection connection;

        [Header("Visual Targets")]

        [SerializeField]
        [Tooltip("Optional Transform oriented along flow direction and scaled by flow intensity.")]
        private Transform flowIndicator;

        [SerializeField]
        [Tooltip("Optional ParticleSystem whose emission rate scales with absolute applied flow.")]
        private ParticleSystem flowParticles;

        [SerializeField]
        [Tooltip("Optional MeshRenderer enabled while flow is active.")]
        private MeshRenderer flowMesh;

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
        [Tooltip("Local scale multiplier applied to the flow indicator at full intensity.")]
        [Min(0f)]
        private float indicatorScaleAtFullFlow = 2f;

        [SerializeField]
        [Tooltip("Particle emission rate at full flow intensity, in particles per second.")]
        [Min(0f)]
        private float particleEmissionAtFullFlow = 40f;

        private Vector3 indicatorBaseScale = Vector3.one;
        private float baseParticleRate;
        private bool hasBaseParticleRate;

        /// <summary>
        /// Gets or sets the connection that drives this visual.
        /// </summary>
        public FloodConnection Connection
        {
            get => connection;
            set => connection = value;
        }

        /// <summary>
        /// Gets or sets the optional flow-direction indicator Transform.
        /// </summary>
        public Transform FlowIndicator
        {
            get => flowIndicator;
            set
            {
                flowIndicator = value;
                if (flowIndicator != null)
                    indicatorBaseScale = flowIndicator.localScale;
            }
        }

        /// <summary>
        /// Gets or sets the optional particle system driven by flow intensity.
        /// </summary>
        public ParticleSystem FlowParticles
        {
            get => flowParticles;
            set
            {
                flowParticles = value;
                hasBaseParticleRate = false;
                CacheDefaults();
            }
        }

        /// <summary>
        /// Gets or sets the optional mesh renderer enabled while flowing.
        /// </summary>
        public MeshRenderer FlowMesh
        {
            get => flowMesh;
            set => flowMesh = value;
        }

        /// <summary>
        /// Gets the latest 0–1 flow intensity applied to presentation targets.
        /// </summary>
        public float CurrentIntensity { get; private set; }

        private void Awake()
        {
            CacheDefaults();
        }

        private void OnEnable()
        {
            if (connection == null)
                connection = GetComponent<FloodConnection>();

            CacheDefaults();
            ApplyPresentation(0f, Vector3.forward, false);
        }

        private void OnDisable()
        {
            ApplyPresentation(0f, Vector3.forward, false);
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
            indicatorScaleAtFullFlow =
                SanitizeNonNegative(indicatorScaleAtFullFlow, 2f);
            particleEmissionAtFullFlow =
                SanitizeNonNegative(particleEmissionAtFullFlow, 40f);

            if (connection == null)
                connection = GetComponent<FloodConnection>();
        }

        /// <summary>
        /// Immediately refreshes presentation from the current connection
        /// diagnostics.
        /// </summary>
        public void Refresh()
        {
            if (connection == null || !isActiveAndEnabled)
            {
                CurrentIntensity = 0f;
                ApplyPresentation(0f, Vector3.forward, false);
                return;
            }

            var rate = connection.CurrentFlowRate;
            var flowing = FloodPresentationUtility.IsFlowing(rate);
            CurrentIntensity = FloodPresentationUtility.FlowIntensity(
                System.Math.Abs(rate),
                lowFlowThreshold,
                highFlowThreshold);

            var direction = connection.FlowDirectionWorld;
            if (direction.sqrMagnitude <= 0.0001f)
                direction = transform.forward;

            ApplyPresentation(CurrentIntensity, direction, flowing);
        }

        private void CacheDefaults()
        {
            if (flowIndicator != null)
                indicatorBaseScale = flowIndicator.localScale;

            if (flowParticles != null && !hasBaseParticleRate)
            {
                var emission = flowParticles.emission;
                baseParticleRate = emission.rateOverTime.constant;
                hasBaseParticleRate = true;
            }
        }

        private void ApplyPresentation(
            float intensity,
            Vector3 directionWorld,
            bool flowing)
        {
            if (flowIndicator != null)
            {
                if (directionWorld.sqrMagnitude > 0.0001f)
                {
                    flowIndicator.rotation = Quaternion.LookRotation(
                        directionWorld.normalized,
                        Vector3.up);
                }

                var scale =
                    1f
                    + ((Mathf.Max(0f, indicatorScaleAtFullFlow) - 1f)
                        * intensity);
                flowIndicator.localScale = indicatorBaseScale * scale;
                flowIndicator.gameObject.SetActive(flowing || intensity > 0f);
            }

            if (flowParticles != null)
            {
                var emission = flowParticles.emission;
                emission.enabled = flowing;
                var rate = flowing
                    ? Mathf.Lerp(
                        0f,
                        Mathf.Max(baseParticleRate, particleEmissionAtFullFlow),
                        intensity)
                    : 0f;
                emission.rateOverTime = rate;

                if (flowing && !flowParticles.isPlaying)
                    flowParticles.Play(true);
                else if (!flowing && flowParticles.isPlaying)
                    flowParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            if (flowMesh != null)
                flowMesh.enabled = flowing;
        }

        private static float SanitizeNonNegative(float value, float fallback)
        {
            return float.IsNaN(value) || float.IsInfinity(value)
                ? fallback
                : Mathf.Max(0f, value);
        }
    }
}
