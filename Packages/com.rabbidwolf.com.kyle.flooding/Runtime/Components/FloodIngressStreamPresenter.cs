using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Lightweight presentation-only stream visual driven by an ingress sample.
    /// </summary>
    /// <remarks>
    /// Scales with flow rate and fades when inflow stops. Does not simulate
    /// collision-based spray or foam.
    /// </remarks>
    [DisallowMultipleComponent]
    [AddComponentMenu("Flooding/Flood Ingress Stream Presenter")]
    public sealed class FloodIngressStreamPresenter : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [Header("Visual")]

        [SerializeField]
        [Tooltip("Optional stream mesh renderer. When unset, a child quad is created at runtime.")]
        private MeshRenderer streamRenderer;

        [SerializeField]
        [Tooltip("Optional particle system whose emission rate scales with flow. Keep simple; no collision spray.")]
        private ParticleSystem splashParticles;

        [SerializeField]
        [Tooltip("Material used when a runtime stream mesh is created.")]
        private Material streamMaterial;

        [SerializeField]
        [Tooltip("Stream color. Alpha is multiplied by flow-driven opacity.")]
        private Color streamColor = new(0.25f, 0.55f, 0.85f, 0.65f);

        [Header("Response")]

        [SerializeField]
        [Tooltip("Seconds used to fade the stream out after inflow stops.")]
        [Min(0f)]
        private float fadeOutSeconds = 0.35f;

        private Transform streamTransform;
        private MaterialPropertyBlock propertyBlock;
        private float baseParticleRate;
        private bool hasBaseParticleRate;
        private float displayStrength;
        private bool ownsRuntimeMesh;

        /// <summary>
        /// Gets or sets the optional authored stream renderer.
        /// </summary>
        public MeshRenderer StreamRenderer
        {
            get => streamRenderer;
            set => streamRenderer = value;
        }

        /// <summary>
        /// Gets or sets the optional splash particle system.
        /// </summary>
        public ParticleSystem SplashParticles
        {
            get => splashParticles;
            set
            {
                splashParticles = value;
                hasBaseParticleRate = false;
                CacheParticleRate();
            }
        }

        /// <summary>
        /// Gets or sets the material used for a runtime-created stream mesh.
        /// </summary>
        public Material StreamMaterial
        {
            get => streamMaterial;
            set => streamMaterial = value;
        }

        /// <summary>
        /// Gets the current 0–1 display strength after fade smoothing.
        /// </summary>
        public float DisplayStrength => displayStrength;

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
            EnsureStreamVisual();
            CacheParticleRate();
            ApplyVisual(0f, transform.position, transform.forward, 1f, 0.1f, 0f);
        }

        private void OnDisable()
        {
            ApplyVisual(0f, transform.position, transform.forward, 1f, 0.1f, 0f);
        }

        private void OnDestroy()
        {
            if (ownsRuntimeMesh && streamTransform != null)
                Destroy(streamTransform.gameObject);
        }

        /// <summary>
        /// Applies a stream visual from the latest ingress sample and profile.
        /// </summary>
        public void Apply(
            in FloodIngressSample sample,
            FloodIngressPresentationProfile profile,
            float deltaTime)
        {
            if (!isActiveAndEnabled || profile == null)
            {
                Hide(deltaTime);
                return;
            }

            var target = profile.EvaluateStreamScale(sample.FlowRateCubicMetersPerSecond);
            displayStrength = MoveTowards(
                displayStrength,
                target,
                deltaTime <= 0f ? 1f : deltaTime / Mathf.Max(0.01f, fadeOutSeconds));

            var length = profile.StreamLengthMeters * Mathf.Max(displayStrength, 0.01f);
            var width = profile.StreamWidthMeters * Mathf.Max(displayStrength, 0.05f);
            var splash = profile.EvaluateSplashStrength(sample.FlowRateCubicMetersPerSecond)
                * displayStrength;

            ApplyVisual(
                displayStrength,
                sample.WorldPosition,
                sample.DirectionWorld,
                length,
                width,
                splash);
        }

        /// <summary>
        /// Fades the stream toward hidden.
        /// </summary>
        public void Hide(float deltaTime)
        {
            displayStrength = MoveTowards(
                displayStrength,
                0f,
                deltaTime <= 0f ? 1f : deltaTime / Mathf.Max(0.01f, fadeOutSeconds));

            ApplyVisual(
                displayStrength,
                transform.position,
                transform.forward,
                1f,
                0.1f,
                0f);
        }

        private void EnsureStreamVisual()
        {
            if (streamRenderer != null)
            {
                streamTransform = streamRenderer.transform;
                return;
            }

            var streamObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            streamObject.name = "Ingress Stream";
            streamObject.transform.SetParent(transform, false);
            Object.Destroy(streamObject.GetComponent<Collider>());
            streamRenderer = streamObject.GetComponent<MeshRenderer>();
            if (streamMaterial != null)
                streamRenderer.sharedMaterial = streamMaterial;
            streamTransform = streamObject.transform;
            ownsRuntimeMesh = true;
        }

        private void CacheParticleRate()
        {
            if (splashParticles == null || hasBaseParticleRate)
                return;

            var emission = splashParticles.emission;
            baseParticleRate = emission.rateOverTime.constant;
            hasBaseParticleRate = true;
        }

        private void ApplyVisual(
            float strength,
            Vector3 worldPosition,
            Vector3 directionWorld,
            float length,
            float width,
            float splashStrength)
        {
            EnsureStreamVisual();
            propertyBlock ??= new MaterialPropertyBlock();

            var active = strength > 0.001f;
            if (streamRenderer != null)
                streamRenderer.enabled = active;

            if (streamTransform != null && active)
            {
                var direction = directionWorld.sqrMagnitude > 0.0001f
                    ? directionWorld.normalized
                    : transform.forward;
                streamTransform.position = worldPosition + (direction * (length * 0.5f));
                streamTransform.rotation = Quaternion.LookRotation(
                    direction,
                    Vector3.up);
                streamTransform.localScale = new Vector3(
                    Mathf.Max(0.01f, width),
                    Mathf.Max(0.01f, width),
                    Mathf.Max(0.01f, length));
            }

            if (streamRenderer != null)
            {
                var color = streamColor;
                color.a *= Mathf.Clamp01(strength);
                streamRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorId, color);
                propertyBlock.SetColor(ColorId, color);
                streamRenderer.SetPropertyBlock(propertyBlock);
            }

            if (splashParticles != null)
            {
                CacheParticleRate();
                var emission = splashParticles.emission;
                var flowing = splashStrength > 0.001f;
                emission.enabled = flowing;
                emission.rateOverTime = flowing
                    ? Mathf.Max(baseParticleRate, 8f) * splashStrength
                    : 0f;

                if (flowing && !splashParticles.isPlaying)
                    splashParticles.Play(true);
                else if (!flowing && splashParticles.isPlaying)
                    splashParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private static float MoveTowards(float current, float target, float maxDelta)
        {
            if (Mathf.Abs(target - current) <= maxDelta)
                return target;

            return current + (Mathf.Sign(target - current) * maxDelta);
        }

        private void OnValidate()
        {
            fadeOutSeconds = float.IsNaN(fadeOutSeconds) || float.IsInfinity(fadeOutSeconds)
                ? 0.35f
                : Mathf.Max(0f, fadeOutSeconds);
        }
    }
}
