using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Kyle.Flooding.URP
{
    /// <summary>
    /// Optional URP renderer feature that applies flood underwater presentation
    /// from <see cref="FloodUnderwaterCameraEffect"/> /
    /// <see cref="FloodCameraTracker"/> state.
    /// </summary>
    /// <remarks>
    /// Camera effects are presentation consumers of Flooding state and do not
    /// participate in simulation. Requires the camera depth texture
    /// (<see cref="ScriptableRenderPassInput.Depth"/>) for world-position
    /// reconstruction and plane waterline masking.
    /// </remarks>
    public sealed class FloodUnderwaterRendererFeature : ScriptableRendererFeature
    {
        [SerializeField]
        [Tooltip("Material using the Kyle/Flooding/Underwater shader. Assign the package FloodUnderwater material or a custom material with that shader.")]
        private Material material;

        [SerializeField]
        [Tooltip("Injection point for the fullscreen underwater pass.")]
        private RenderPassEvent renderPassEvent =
            RenderPassEvent.BeforeRenderingPostProcessing;

        [SerializeField]
        [Tooltip("Softness of the waterline mask in meters of signed surface distance. Larger values widen the blend band across the flood surface plane.")]
        [Min(0.0001f)]
        private float waterlineSoftnessMeters = 0.03f;

        private FloodUnderwaterPass pass;

        /// <summary>
        /// Gets or sets the underwater blit material.
        /// </summary>
        public Material Material
        {
            get => material;
            set => material = value;
        }

        /// <inheritdoc />
        public override void Create()
        {
            pass = new FloodUnderwaterPass
            {
                renderPassEvent = renderPassEvent,
            };
        }

        /// <inheritdoc />
        public override void AddRenderPasses(
            ScriptableRenderer renderer,
            ref RenderingData renderingData)
        {
            if (material == null)
                return;

            var camera = renderingData.cameraData.camera;
            if (camera == null)
                return;

            if (renderingData.cameraData.cameraType == CameraType.Preview
                || renderingData.cameraData.cameraType == CameraType.Reflection
                || UniversalRenderer.IsOffscreenDepthTexture(
                    ref renderingData.cameraData))
            {
                return;
            }

            var effect = camera.GetComponent<FloodUnderwaterCameraEffect>();
            if (effect == null || !effect.CanRender)
                return;

            pass.renderPassEvent = renderPassEvent;
            pass.Setup(material, effect, waterlineSoftnessMeters);
            renderer.EnqueuePass(pass);
        }
    }
}
