using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace Kyle.Flooding.URP
{
    /// <summary>
    /// Fullscreen URP render-graph pass that applies underwater tint/fog and a
    /// camera-ray / surface-plane waterline using depth reconstruction.
    /// </summary>
    /// <remarks>
    /// The shader traces each pixel ray against <see cref="FloodVolume.SurfacePlane"/>
    /// and combines that with scene depth so open views (sky / far plane) still
    /// receive a waterline. Fog strength uses underwater optical path length
    /// along the view ray. The active surface is still treated as an infinite
    /// plane; it is not clipped to FloodVolume bounds.
    /// </remarks>
    public sealed class FloodUnderwaterPass : ScriptableRenderPass
    {
        private static readonly int SurfaceNormalId =
            Shader.PropertyToID("_FloodSurfaceNormal");
        private static readonly int SurfacePlaneDId =
            Shader.PropertyToID("_FloodSurfacePlaneD");
        private static readonly int ShallowTintId =
            Shader.PropertyToID("_FloodShallowTint");
        private static readonly int DeepTintId =
            Shader.PropertyToID("_FloodDeepTint");
        private static readonly int FullEffectDepthId =
            Shader.PropertyToID("_FloodFullEffectDepth");
        private static readonly int FogDensityId =
            Shader.PropertyToID("_FloodFogDensity");
        private static readonly int MaxFogId =
            Shader.PropertyToID("_FloodMaxFog");
        private static readonly int SaturationId =
            Shader.PropertyToID("_FloodSaturation");
        private static readonly int ContrastId =
            Shader.PropertyToID("_FloodContrast");
        private static readonly int DistortionStrengthId =
            Shader.PropertyToID("_FloodDistortionStrength");
        private static readonly int DistortionSpeedId =
            Shader.PropertyToID("_FloodDistortionSpeed");
        private static readonly int EffectBlendId =
            Shader.PropertyToID("_FloodEffectBlend");
        private static readonly int CameraSubmersionId =
            Shader.PropertyToID("_FloodCameraSubmersion");
        private static readonly int CameraUnderwaterId =
            Shader.PropertyToID("_FloodCameraUnderwater");
        private static readonly int WaterlineSoftnessId =
            Shader.PropertyToID("_FloodWaterlineSoftness");

        private readonly MaterialPropertyBlock propertyBlock = new();
        private Material material;
        private FloodUnderwaterCameraEffect effect;
        private float waterlineSoftnessMeters = 0.03f;

        /// <summary>
        /// Creates the underwater pass.
        /// </summary>
        public FloodUnderwaterPass()
        {
            profilingSampler = new ProfilingSampler("Flood Underwater");
            requiresIntermediateTexture = true;
        }

        /// <summary>
        /// Configures material and per-camera effect sources for this frame.
        /// </summary>
        public void Setup(
            Material underwaterMaterial,
            FloodUnderwaterCameraEffect cameraEffect,
            float waterlineSoftness)
        {
            material = underwaterMaterial;
            effect = cameraEffect;
            waterlineSoftnessMeters = Mathf.Max(0.0001f, waterlineSoftness);
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        /// <inheritdoc />
        public override void RecordRenderGraph(
            RenderGraph renderGraph,
            ContextContainer frameData)
        {
            if (material == null || effect == null || !effect.CanRender)
                return;

            var tracker = effect.Tracker;
            var profile = effect.Profile;
            if (tracker == null || profile == null || tracker.ActiveVolume == null)
                return;

            var resourceData = frameData.Get<UniversalResourceData>();
            if (resourceData.isActiveTargetBackBuffer)
                return;

            var source = resourceData.activeColorTexture;
            if (!source.IsValid())
                return;

            var destinationDesc = renderGraph.GetTextureDesc(source);
            destinationDesc.name = "CameraColor-FloodUnderwater";
            destinationDesc.clearBuffer = false;
            var destination = renderGraph.CreateTexture(destinationDesc);

            ApplyProperties(tracker, profile, effect.EffectBlend);

            var blitParams = new RenderGraphUtils.BlitMaterialParameters(
                source,
                destination,
                material,
                0)
            {
                propertyBlock = propertyBlock,
            };

            renderGraph.AddBlitPass(blitParams, passName: "Flood Underwater");
            resourceData.cameraColor = destination;
        }

        private void ApplyProperties(
            FloodCameraTracker tracker,
            FloodUnderwaterProfile profile,
            float effectBlend)
        {
            var plane = tracker.ActiveVolume.SurfacePlane;
            var normal = plane.normal.normalized;
            // Unity Plane: distance(point) = dot(normal, point) + distance
            var planeD = plane.distance;

            propertyBlock.Clear();
            propertyBlock.SetVector(
                SurfaceNormalId,
                new Vector4(normal.x, normal.y, normal.z, 0f));
            propertyBlock.SetFloat(SurfacePlaneDId, planeD);
            propertyBlock.SetColor(ShallowTintId, profile.ShallowTintColor);
            propertyBlock.SetColor(DeepTintId, profile.DeepTintColor);
            propertyBlock.SetFloat(
                FullEffectDepthId,
                profile.FullEffectDepthMeters);
            propertyBlock.SetFloat(FogDensityId, profile.FogDensity);
            propertyBlock.SetFloat(MaxFogId, profile.MaximumFogStrength);
            propertyBlock.SetFloat(SaturationId, profile.Saturation);
            propertyBlock.SetFloat(ContrastId, profile.Contrast);
            propertyBlock.SetFloat(
                DistortionStrengthId,
                profile.DistortionStrength);
            propertyBlock.SetFloat(DistortionSpeedId, profile.DistortionSpeed);
            propertyBlock.SetFloat(EffectBlendId, Mathf.Clamp01(effectBlend));
            propertyBlock.SetFloat(
                CameraSubmersionId,
                Mathf.Max(0f, tracker.SubmersionDepthMeters));
            propertyBlock.SetFloat(
                CameraUnderwaterId,
                tracker.IsUnderwater ? 1f : 0f);
            propertyBlock.SetFloat(WaterlineSoftnessId, waterlineSoftnessMeters);
        }
    }
}
