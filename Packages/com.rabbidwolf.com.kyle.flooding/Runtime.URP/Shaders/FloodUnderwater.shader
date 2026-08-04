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

            float SurfaceSignedDistance(float3 worldPosition)
            {
                return dot(worldPosition, _FloodSurfaceNormal.xyz) + _FloodSurfacePlaneD;
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
                float3 worldPos = ReconstructWorldPosition(uv, deviceDepth);
                float signedDistance = SurfaceSignedDistance(worldPos);

                // Far-plane / sky pixels: fall back to latched camera underwater state
                // so the whole view tints when fully submerged.
                #if UNITY_REVERSED_Z
                float isFar = deviceDepth <= 1e-5 ? 1.0 : 0.0;
                #else
                float isFar = deviceDepth >= 0.999999 ? 1.0 : 0.0;
                #endif

                float softness = max(_FloodWaterlineSoftness, 1e-4);
                float geometryMask = 1.0 - smoothstep(-softness, softness, signedDistance);
                float underwaterMask = lerp(geometryMask, _FloodCameraUnderwater, isFar);
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

                float pixelSubmersion = max(0.0, -signedDistance);
                float submersion = lerp(pixelSubmersion, max(pixelSubmersion, _FloodCameraSubmersion), isFar);
                float fullDepth = max(_FloodFullEffectDepth, 1e-4);
                float depthStrength = saturate(submersion / fullDepth);

                // Approaching / shallow: keep a little tint even with small submersion.
                float shallowBoost = saturate((-signedDistance + softness) / (softness * 2.0));
                depthStrength = max(depthStrength, shallowBoost * 0.25 * underwaterMask);

                float4 tint = lerp(_FloodShallowTint, _FloodDeepTint, depthStrength);
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
