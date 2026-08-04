Shader "Kyle/Flooding/Underwater"
{
    Properties
    {
        [HideInInspector] _MainTex ("Source", 2D) = "white" {}
        _FloodWaterlineSoftness ("Waterline Softness", Float) = 0.03
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "FloodUnderwater"

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float4 _FloodSurfaceNormal;
            float _FloodSurfacePlaneD;
            float4 _FloodShallowTint;
            float4 _FloodDeepTint;
            float _FloodFullEffectDepth;
            float _FloodFogDensity;
            float _FloodMaxFog;
            float _FloodSaturation;
            float _FloodContrast;
            float _FloodDistortionStrength;
            float _FloodDistortionSpeed;
            float _FloodEffectBlend;
            float _FloodCameraSubmersion;
            float _FloodCameraUnderwater;
            float _FloodWaterlineSoftness;

            float3 ApplySaturation(float3 color, float saturation)
            {
                float luma = dot(color, float3(0.2126, 0.7152, 0.0722));
                return lerp(luma.xxx, color, saturation);
            }

            float3 ApplyContrast(float3 color, float contrast)
            {
                return (color - 0.5) * contrast + 0.5;
            }

            float SampleDeviceDepth(float2 uv)
            {
                return SampleSceneDepth(uv);
            }

            float3 ReconstructWorldPosition(float2 uv, float deviceDepth)
            {
                return ComputeWorldSpacePosition(uv, deviceDepth, UNITY_MATRIX_I_VP);
            }

            // Positive above surface, negative below (matches FloodQueryResult).
            float SurfaceSignedDistance(float3 worldPosition)
            {
                return dot(worldPosition, _FloodSurfaceNormal.xyz) + _FloodSurfacePlaneD;
            }

            // Length of the camera→scene segment that lies in the water half-space
            // (signed distance < 0). Linear sd along the ray means at most one crossing.
            float UnderwaterPathLength(float cameraSigned, float sceneSigned, float tScene)
            {
                tScene = max(tScene, 0.0);
                bool startWet = cameraSigned < 0.0;
                bool endWet = sceneSigned < 0.0;

                if (startWet && endWet)
                    return tScene;

                if (!startWet && !endWet)
                    return 0.0;

                float denom = sceneSigned - cameraSigned;
                if (abs(denom) < 1e-6)
                    return startWet ? tScene : 0.0;

                float tCross = saturate(-cameraSigned / denom) * tScene;
                return startWet ? tCross : (tScene - tCross);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                float effectBlend = saturate(_FloodEffectBlend);
                if (effectBlend <= 1e-4)
                {
                    return half4(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb, 1.0);
                }

                float deviceDepth = SampleDeviceDepth(uv);
                float3 sceneWorldPos = ReconstructWorldPosition(uv, deviceDepth);
                float3 cameraWorldPos = _WorldSpaceCameraPos;
                float3 toScene = sceneWorldPos - cameraWorldPos;
                float tScene = length(toScene);

                #if UNITY_REVERSED_Z
                float isFar = deviceDepth <= 1e-5 ? 1.0 : 0.0;
                #else
                float isFar = deviceDepth >= 0.999999 ? 1.0 : 0.0;
                #endif

                // For sky / far-plane pixels, keep a stable ray length for path
                // classification so open views still get a camera-ray waterline.
                if (isFar > 0.5)
                    tScene = max(tScene, _ProjectionParams.z);

                float3 rayDir = tScene > 1e-5
                    ? toScene / max(tScene, 1e-5)
                    : float3(0.0, 0.0, 1.0);

                // Re-derive scene point from ray when far so sd uses the frustum
                // far hit rather than a degenerate reconstruct.
                float3 evalScenePos = cameraWorldPos + rayDir * tScene;
                float cameraSigned = SurfaceSignedDistance(cameraWorldPos);
                float sceneSigned = SurfaceSignedDistance(evalScenePos);
                float pathLength = UnderwaterPathLength(
                    cameraSigned,
                    sceneSigned,
                    tScene);

                float softness = max(_FloodWaterlineSoftness, 1e-4);
                // Soft waterline: short underwater paths near the crossing fade in.
                float pathMask = smoothstep(0.0, softness, pathLength);
                // Also soften against scene signed distance so wall waterlines
                // match enclosed-compartment looks from v1.
                float geometryMask = 1.0 - smoothstep(-softness, softness, sceneSigned);
                float underwaterMask = max(pathMask, geometryMask * (1.0 - isFar));
                // Fully submerged camera: keep residual coverage if path is tiny
                // due to numerical issues near the surface.
                underwaterMask = max(
                    underwaterMask,
                    _FloodCameraUnderwater * effectBlend * isFar);
                underwaterMask *= effectBlend;

                if (underwaterMask <= 1e-4)
                {
                    return half4(SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb, 1.0);
                }

                float time = _Time.y * _FloodDistortionSpeed;
                float2 distortion = float2(
                    sin((uv.y + time) * 28.0),
                    cos((uv.x + time * 0.85) * 24.0)) * (_FloodDistortionStrength * underwaterMask);
                float2 distortedUv = saturate(uv + distortion);

                float3 source = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, distortedUv).rgb;

                // Intensity from optical path through water (meters along view ray),
                // not perpendicular plane depth — oblique views fog more correctly.
                float fullDepth = max(_FloodFullEffectDepth, 1e-4);
                float pathForStrength = pathLength;
                if (isFar > 0.5 && _FloodCameraUnderwater > 0.5)
                    pathForStrength = max(pathForStrength, _FloodCameraSubmersion);

                float depthStrength = saturate(pathForStrength / fullDepth);
                float shallowBoost = saturate(pathLength / (softness * 2.0));
                depthStrength = max(depthStrength, shallowBoost * 0.25 * underwaterMask);

                float4 tint = lerp(_FloodShallowTint, _FloodDeepTint, depthStrength);
                // Shape fog with the same density*depthStrength curve as
                // FloodUnderwaterProfile.EvaluateFogStrength, but feed optical
                // path (via depthStrength) instead of vertical submersion.
                float fog = _FloodMaxFog * (1.0 - exp(-_FloodFogDensity * 6.0 * depthStrength));
                fog = saturate(fog) * underwaterMask;

                float3 graded = ApplyContrast(
                    ApplySaturation(source, lerp(1.0, _FloodSaturation, underwaterMask)),
                    lerp(1.0, _FloodContrast, underwaterMask));

                float3 tinted = lerp(graded, tint.rgb, saturate(tint.a) * underwaterMask);
                float3 color = lerp(tinted, tint.rgb, fog);

                return half4(lerp(source, color, underwaterMask), 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
