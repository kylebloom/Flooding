using UnityEngine;

namespace Kyle.Flooding
{
    /// <summary>
    /// Presentation-only ballistic water jet and optional impact splash driven by
    /// an ingress sample. Does not use Rigidbody particles for the primary jet.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Flooding/Flood Ingress Stream Presenter")]
    public sealed class FloodIngressStreamPresenter : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int FlowSpeedId = Shader.PropertyToID("_FlowSpeed");
        private static readonly int TurbulenceId = Shader.PropertyToID("_Turbulence");
        private static readonly int OpacityId = Shader.PropertyToID("_Opacity");
        private static readonly int StrengthId = Shader.PropertyToID("_Strength");

        [Header("Visual")]

        [SerializeField]
        [Tooltip("Optional mesh renderer for the procedural jet. When unset, a child MeshFilter/MeshRenderer is created at runtime.")]
        private MeshRenderer streamRenderer;

        [SerializeField]
        [Tooltip("Optional particle system placed at the jet impact region. Emission scales with flow; no collision fluid simulation.")]
        private ParticleSystem splashParticles;

        [SerializeField]
        [Tooltip("Material for the procedural jet. Prefer Kyle/Flooding/Ingress Jet under URP; Lit/transparent materials remain a valid fallback.")]
        private Material streamMaterial;

        [SerializeField]
        [Tooltip("Jet color used when the material exposes _BaseColor/_Color. Alpha is multiplied by display strength.")]
        private Color streamColor = new(0.35f, 0.7f, 0.95f, 0.75f);

        [Header("Simulation Context")]

        [SerializeField]
        [Tooltip("Optional manager used for ActiveGravity. When unset, Physics.gravity is used.")]
        private FloodSimulationManager simulationManager;

        [SerializeField]
        [Tooltip("Presentation floor plane for impact projection. Position is a point on the floor; up is the floor normal.")]
        private Transform floorPlane;

        [Header("Response")]

        [SerializeField]
        [Tooltip("Seconds used to fade the jet out after inflow stops.")]
        [Min(0f)]
        private float fadeOutSeconds = 0.25f;

        private FloodIngressJetMesh jetMesh;
        private MeshFilter meshFilter;
        private Transform streamTransform;
        private MaterialPropertyBlock propertyBlock;
        private ParticleSystem.MainModule splashMain;
        private ParticleSystem.EmissionModule splashEmission;
        private ParticleSystem.ShapeModule splashShape;
        private float baseParticleRate = 24f;
        private float baseStartSpeed = 1.5f;
        private float baseStartSize = 0.08f;
        private bool hasCachedParticles;
        private float displayStrength;
        private bool ownsRuntimeMesh;
        private Vector3 lastImpactPoint;
        private bool hasImpact;

        /// <summary>
        /// Gets or sets the optional authored jet renderer.
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
                hasCachedParticles = false;
                CacheParticleDefaults();
            }
        }

        /// <summary>
        /// Gets or sets the jet material.
        /// </summary>
        public Material StreamMaterial
        {
            get => streamMaterial;
            set
            {
                streamMaterial = value;
                if (streamRenderer != null && streamMaterial != null)
                    streamRenderer.sharedMaterial = streamMaterial;
            }
        }

        /// <summary>
        /// Gets or sets the floor plane used for impact projection.
        /// </summary>
        public Transform FloorPlane
        {
            get => floorPlane;
            set => floorPlane = value;
        }

        /// <summary>
        /// Gets or sets the simulation manager used for gravity.
        /// </summary>
        public FloodSimulationManager SimulationManager
        {
            get => simulationManager;
            set => simulationManager = value;
        }

        /// <summary>
        /// Gets the current 0–1 display strength after fade smoothing.
        /// </summary>
        public float DisplayStrength => displayStrength;

        /// <summary>
        /// Gets the latest predicted impact point.
        /// </summary>
        public Vector3 ImpactPointWorld => lastImpactPoint;

        /// <summary>
        /// Gets whether the latest deform found a floor impact.
        /// </summary>
        public bool HasImpact => hasImpact;

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
            EnsureJetVisual();
            CacheParticleDefaults();
            ApplyHidden();
        }

        private void OnDisable()
        {
            ApplyHidden();
        }

        private void OnDestroy()
        {
            if (ownsRuntimeMesh && streamTransform != null)
                Destroy(streamTransform.gameObject);
        }

        /// <summary>
        /// Applies a ballistic jet visual from the latest ingress sample.
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

            if (displayStrength <= 0.001f)
            {
                ApplyHidden();
                return;
            }

            EnsureJetVisual();
            CacheParticleDefaults();

            var gravity = ResolveGravity() * profile.JetGravityInfluence;
            var speed = profile.JetInitialSpeed * Mathf.Max(displayStrength, 0.05f);
            var lifetime = profile.JetLifetimeSeconds * Mathf.Lerp(0.55f, 1f, displayStrength);
            var width = profile.JetWidthMeters * Mathf.Max(displayStrength, 0.08f);
            var floorPoint = floorPlane != null ? floorPlane.position : transform.position;
            var floorNormal = floorPlane != null && floorPlane.up.sqrMagnitude > 0.0001f
                ? floorPlane.up.normalized
                : Vector3.up;

            jetMesh.Deform(
                sample.WorldPosition,
                sample.DirectionWorld,
                gravity,
                speed,
                lifetime,
                width,
                profile.JetTaper,
                floorPoint,
                floorNormal);

            hasImpact = jetMesh.HasImpact;
            lastImpactPoint = jetMesh.ImpactPointWorld;

            if (streamTransform != null)
            {
                streamTransform.SetPositionAndRotation(
                    sample.WorldPosition,
                    Quaternion.identity);
                streamTransform.localScale = Vector3.one;
            }

            if (streamRenderer != null)
            {
                streamRenderer.enabled = true;
                if (streamMaterial != null)
                    streamRenderer.sharedMaterial = streamMaterial;

                propertyBlock ??= new MaterialPropertyBlock();
                var color = streamColor;
                color.a *= Mathf.Clamp01(displayStrength);
                streamRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorId, color);
                propertyBlock.SetColor(ColorId, color);
                propertyBlock.SetFloat(OpacityId, color.a);
                propertyBlock.SetFloat(StrengthId, displayStrength);
                propertyBlock.SetFloat(
                    FlowSpeedId,
                    profile.JetUvFlowSpeed * displayStrength);
                propertyBlock.SetFloat(
                    TurbulenceId,
                    profile.JetTurbulence * displayStrength);
                streamRenderer.SetPropertyBlock(propertyBlock);
            }

            var splashStrength =
                profile.EvaluateSplashStrength(sample.FlowRateCubicMetersPerSecond)
                * displayStrength;
            ApplySplash(profile, splashStrength);
        }

        /// <summary>
        /// Fades the jet toward hidden.
        /// </summary>
        public void Hide(float deltaTime)
        {
            displayStrength = MoveTowards(
                displayStrength,
                0f,
                deltaTime <= 0f ? 1f : deltaTime / Mathf.Max(0.01f, fadeOutSeconds));

            if (displayStrength <= 0.001f)
                ApplyHidden();
        }

        private void ApplyHidden()
        {
            if (streamRenderer != null)
                streamRenderer.enabled = false;

            if (splashParticles != null && splashParticles.isPlaying)
                splashParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            hasImpact = false;
        }

        private void EnsureJetVisual()
        {
            jetMesh ??= new FloodIngressJetMesh();

            if (streamRenderer != null)
            {
                streamTransform = streamRenderer.transform;
                meshFilter = streamRenderer.GetComponent<MeshFilter>();
                if (meshFilter == null)
                    meshFilter = streamRenderer.gameObject.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = jetMesh.Mesh;
                if (streamMaterial != null)
                    streamRenderer.sharedMaterial = streamMaterial;
                return;
            }

            var jetObject = new GameObject("Ingress Jet");
            jetObject.transform.SetParent(transform, false);
            meshFilter = jetObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = jetMesh.Mesh;
            streamRenderer = jetObject.AddComponent<MeshRenderer>();
            streamRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            streamRenderer.receiveShadows = false;
            if (streamMaterial != null)
                streamRenderer.sharedMaterial = streamMaterial;
            streamTransform = jetObject.transform;
            ownsRuntimeMesh = true;
        }

        private void CacheParticleDefaults()
        {
            if (splashParticles == null || hasCachedParticles)
                return;

            splashMain = splashParticles.main;
            splashEmission = splashParticles.emission;
            splashShape = splashParticles.shape;
            baseParticleRate = splashEmission.rateOverTime.constant;
            if (baseParticleRate <= 0f)
                baseParticleRate = 24f;
            baseStartSpeed = splashMain.startSpeed.constant;
            if (baseStartSpeed <= 0f)
                baseStartSpeed = 1.5f;
            baseStartSize = splashMain.startSize.constant;
            if (baseStartSize <= 0f)
                baseStartSize = 0.08f;
            hasCachedParticles = true;
        }

        private void ApplySplash(
            FloodIngressPresentationProfile profile,
            float splashStrength)
        {
            if (splashParticles == null)
                return;

            CacheParticleDefaults();
            var flowing = splashStrength > 0.02f && hasImpact;
            splashEmission.enabled = flowing;
            splashEmission.rateOverTime = flowing
                ? baseParticleRate
                    * profile.SplashEmissionMultiplier
                    * splashStrength
                : 0f;

            if (flowing)
            {
                splashParticles.transform.position = lastImpactPoint;
                if (floorPlane != null)
                {
                    splashParticles.transform.rotation = Quaternion.LookRotation(
                        floorPlane.up,
                        Vector3.forward);
                }

                splashMain.startSpeed = baseStartSpeed
                    * profile.SplashDropletSpeed
                    * Mathf.Lerp(0.35f, 1.25f, splashStrength);
                splashMain.startSize = baseStartSize
                    * profile.SplashDropletSize
                    * Mathf.Lerp(0.5f, 1.35f, splashStrength);
                splashShape.angle = Mathf.Lerp(12f, 35f, splashStrength);

                if (!splashParticles.isPlaying)
                    splashParticles.Play(true);
            }
            else if (splashParticles.isPlaying)
            {
                splashParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private Vector3 ResolveGravity()
        {
            if (simulationManager != null)
                return simulationManager.ActiveGravity;

            return Physics.gravity;
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
                ? 0.25f
                : Mathf.Max(0f, fadeOutSeconds);
        }
    }
}
