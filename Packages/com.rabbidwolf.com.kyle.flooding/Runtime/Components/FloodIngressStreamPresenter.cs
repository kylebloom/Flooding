using UnityEngine;
using UnityEngine.Serialization;

namespace Kyle.Flooding
{
    /// <summary>
    /// Presentation-only ballistic water jet and layered impact particles driven
    /// by an ingress sample. Does not use Rigidbody particles for the primary jet.
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
        [FormerlySerializedAs("splashParticles")]
        [Tooltip("Ballistic droplet ParticleSystem at the jet impact region. Prefer stretched soft-alpha billboards. Emission scales with flow.")]
        private ParticleSystem dropletParticles;

        [SerializeField]
        [Tooltip("Optional soft spray/mist ParticleSystem for medium and major flow. Leave empty to skip mist.")]
        private ParticleSystem sprayMistParticles;

        [SerializeField]
        [Tooltip("Optional whitewater foam-burst ParticleSystem near the impact surface. Leave empty to skip foam particles.")]
        private ParticleSystem foamBurstParticles;

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
        private ImpactLayerCache droplets;
        private ImpactLayerCache sprayMist;
        private ImpactLayerCache foamBurst;
        private float displayStrength;
        private bool ownsRuntimeMesh;
        private Vector3 lastImpactPoint;
        private bool hasImpact;

        private struct ImpactLayerCache
        {
            public ParticleSystem System;
            public ParticleSystem.MainModule Main;
            public ParticleSystem.EmissionModule Emission;
            public ParticleSystem.ShapeModule Shape;
            public float BaseRate;
            public float BaseStartSpeed;
            public float BaseStartSize;
            public bool Cached;
        }

        /// <summary>
        /// Gets or sets the optional authored jet renderer.
        /// </summary>
        public MeshRenderer StreamRenderer
        {
            get => streamRenderer;
            set => streamRenderer = value;
        }

        /// <summary>
        /// Gets or sets the ballistic droplet particle system (primary splash layer).
        /// </summary>
        public ParticleSystem DropletParticles
        {
            get => dropletParticles;
            set
            {
                dropletParticles = value;
                droplets.Cached = false;
            }
        }

        /// <summary>
        /// Compatibility alias for <see cref="DropletParticles"/>.
        /// </summary>
        public ParticleSystem SplashParticles
        {
            get => dropletParticles;
            set => DropletParticles = value;
        }

        /// <summary>
        /// Gets or sets the optional spray/mist particle system.
        /// </summary>
        public ParticleSystem SprayMistParticles
        {
            get => sprayMistParticles;
            set
            {
                sprayMistParticles = value;
                sprayMist.Cached = false;
            }
        }

        /// <summary>
        /// Gets or sets the optional foam-burst particle system.
        /// </summary>
        public ParticleSystem FoamBurstParticles
        {
            get => foamBurstParticles;
            set
            {
                foamBurstParticles = value;
                foamBurst.Cached = false;
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
            CacheImpactLayers();
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
            CacheImpactLayers();

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
            ApplyImpactLayers(profile, splashStrength);
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

            StopLayer(ref droplets);
            StopLayer(ref sprayMist);
            StopLayer(ref foamBurst);
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

        private void CacheImpactLayers()
        {
            CacheLayer(ref droplets, dropletParticles, 36f, 2.2f, 0.09f);
            CacheLayer(ref sprayMist, sprayMistParticles, 40f, 0.9f, 0.22f);
            CacheLayer(ref foamBurst, foamBurstParticles, 24f, 0.55f, 0.28f);
        }

        private static void CacheLayer(
            ref ImpactLayerCache cache,
            ParticleSystem system,
            float defaultRate,
            float defaultSpeed,
            float defaultSize)
        {
            if (system == null)
            {
                cache = default;
                return;
            }

            if (cache.Cached && cache.System == system)
                return;

            cache.System = system;
            cache.Main = system.main;
            cache.Emission = system.emission;
            cache.Shape = system.shape;
            cache.BaseRate = cache.Emission.rateOverTime.constant;
            if (cache.BaseRate <= 0f)
                cache.BaseRate = defaultRate;
            cache.BaseStartSpeed = cache.Main.startSpeed.constant;
            if (cache.BaseStartSpeed <= 0f)
                cache.BaseStartSpeed = defaultSpeed;
            cache.BaseStartSize = cache.Main.startSize.constant;
            if (cache.BaseStartSize <= 0f)
                cache.BaseStartSize = defaultSize;
            cache.Cached = true;
        }

        private void ApplyImpactLayers(
            FloodIngressPresentationProfile profile,
            float splashStrength)
        {
            var flowing = splashStrength > 0.02f && hasImpact;
            var impactRotation = floorPlane != null
                ? Quaternion.LookRotation(floorPlane.up, Vector3.forward)
                : Quaternion.identity;

            ApplyDroplets(profile, splashStrength, flowing, impactRotation);
            ApplySprayMist(profile, splashStrength, flowing, impactRotation);
            ApplyFoamBurst(profile, splashStrength, flowing, impactRotation);
        }

        private void ApplyDroplets(
            FloodIngressPresentationProfile profile,
            float splashStrength,
            bool flowing,
            Quaternion impactRotation)
        {
            if (!droplets.Cached)
                return;

            var active = flowing;
            droplets.Emission.enabled = active;
            droplets.Emission.rateOverTime = active
                ? droplets.BaseRate
                    * profile.SplashEmissionMultiplier
                    * splashStrength
                : 0f;

            if (!active)
            {
                StopLayer(ref droplets);
                return;
            }

            droplets.System.transform.SetPositionAndRotation(lastImpactPoint, impactRotation);
            droplets.Main.startSpeed = droplets.BaseStartSpeed
                * profile.SplashDropletSpeed
                * Mathf.Lerp(0.35f, 1.35f, splashStrength);
            droplets.Main.startSize = droplets.BaseStartSize
                * profile.SplashDropletSize
                * Mathf.Lerp(0.55f, 1.4f, splashStrength);
            droplets.Shape.angle = Mathf.Lerp(14f, 38f, splashStrength);

            if (!droplets.System.isPlaying)
                droplets.System.Play(true);
        }

        private void ApplySprayMist(
            FloodIngressPresentationProfile profile,
            float splashStrength,
            bool flowing,
            Quaternion impactRotation)
        {
            if (!sprayMist.Cached)
                return;

            var mistStrength = Mathf.InverseLerp(
                profile.SprayMistThreshold,
                1f,
                splashStrength);
            var active = flowing && mistStrength > 0.02f;
            sprayMist.Emission.enabled = active;
            sprayMist.Emission.rateOverTime = active
                ? sprayMist.BaseRate
                    * profile.SplashEmissionMultiplier
                    * mistStrength
                : 0f;

            if (!active)
            {
                StopLayer(ref sprayMist);
                return;
            }

            sprayMist.System.transform.SetPositionAndRotation(lastImpactPoint, impactRotation);
            sprayMist.Main.startSpeed = sprayMist.BaseStartSpeed
                * Mathf.Lerp(0.5f, 1.2f, mistStrength);
            sprayMist.Main.startSize = sprayMist.BaseStartSize
                * Mathf.Lerp(0.7f, 1.35f, mistStrength);
            sprayMist.Shape.angle = Mathf.Lerp(28f, 55f, mistStrength);

            if (!sprayMist.System.isPlaying)
                sprayMist.System.Play(true);
        }

        private void ApplyFoamBurst(
            FloodIngressPresentationProfile profile,
            float splashStrength,
            bool flowing,
            Quaternion impactRotation)
        {
            if (!foamBurst.Cached)
                return;

            var foamStrength = Mathf.InverseLerp(
                profile.FoamBurstThreshold,
                1f,
                splashStrength) * profile.FoamStrength;
            var active = flowing && foamStrength > 0.02f;
            foamBurst.Emission.enabled = active;
            foamBurst.Emission.rateOverTime = active
                ? foamBurst.BaseRate
                    * profile.SplashEmissionMultiplier
                    * foamStrength
                : 0f;

            if (!active)
            {
                StopLayer(ref foamBurst);
                return;
            }

            foamBurst.System.transform.SetPositionAndRotation(lastImpactPoint, impactRotation);
            foamBurst.Main.startSpeed = foamBurst.BaseStartSpeed
                * Mathf.Lerp(0.4f, 1.15f, foamStrength);
            foamBurst.Main.startSize = foamBurst.BaseStartSize
                * Mathf.Lerp(0.65f, 1.5f, foamStrength);
            foamBurst.Shape.angle = Mathf.Lerp(40f, 75f, foamStrength);
            foamBurst.Shape.radius = Mathf.Lerp(0.08f, 0.22f, foamStrength);

            if (!foamBurst.System.isPlaying)
                foamBurst.System.Play(true);
        }

        private static void StopLayer(ref ImpactLayerCache cache)
        {
            if (!cache.Cached || cache.System == null)
                return;

            cache.Emission.enabled = false;
            cache.Emission.rateOverTime = 0f;
            if (cache.System.isPlaying)
                cache.System.Stop(true, ParticleSystemStopBehavior.StopEmitting);
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
