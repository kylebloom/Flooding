Shader "Kyle/Flooding/Ingress Patch"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.18, 0.5, 0.82, 0.55)
        _Opacity ("Opacity", Range(0, 1)) = 0.55
        _Strength ("Strength", Range(0, 1)) = 1
        _EdgeNoiseScale ("Edge Noise Scale", Float) = 2.8
        _EdgeNoiseStrength ("Edge Noise Strength", Range(0, 1)) = 0.45
        _EdgeSoftness ("Edge Softness", Range(0.01, 0.5)) = 0.2
        _RippleStrength ("Ripple Strength", Range(0, 1)) = 0.22
        _RippleSpeed ("Ripple Speed", Float) = 1.8
        _FoamColor ("Foam Color", Color) = (0.9, 0.95, 1.0, 1)
        _FoamStrength ("Foam Strength", Range(0, 1)) = 0.75
        _FoamEdgeWidth ("Foam Edge Width", Range(0.01, 0.5)) = 0.18
        _FoamNoiseScale ("Foam Noise Scale", Float) = 4.5
        _FoamScrollSpeed ("Foam Scroll Speed", Float) = 0.65
        _FresnelPower ("Fresnel Power", Range(0.5, 8)) = 3.2
        _FresnelIntensity ("Fresnel Intensity", Range(0, 2)) = 0.4
        _SpecularIntensity ("Specular Intensity", Range(0, 2)) = 0.35
        _NormalStrength ("Normal Strength", Range(0, 2)) = 0.55
        _FlowMotion ("Flow Motion", Float) = 0.85
        _Stretch ("Stretch", Vector) = (1, 1, 0, 0)
        _FlowDirection ("Flow Direction", Vector) = (0, 0, 1, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "IngressPatch"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Opacity;
                float _Strength;
                float _EdgeNoiseScale;
                float _EdgeNoiseStrength;
                float _EdgeSoftness;
                float _RippleStrength;
                float _RippleSpeed;
                float4 _FoamColor;
                float _FoamStrength;
                float _FoamEdgeWidth;
                float _FoamNoiseScale;
                float _FoamScrollSpeed;
                float _FresnelPower;
                float _FresnelIntensity;
                float _SpecularIntensity;
                float _NormalStrength;
                float _FlowMotion;
                float4 _Stretch;
                float4 _FlowDirection;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 19.19);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float Fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                v += ValueNoise(p) * a;
                p = p * 2.1 + 13.7;
                a *= 0.5;
                v += ValueNoise(p) * a;
                return v;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 pos = input.positionOS.xyz;
                float2 centered = input.uv * 2.0 - 1.0;
                float ripple = sin((centered.x * 7.0 + centered.y * 5.0) + _Time.y * _RippleSpeed * 6.283185);
                float ripple2 = sin((centered.x * -4.0 + centered.y * 9.0) + _Time.y * _RippleSpeed * 4.1);
                pos.y += (ripple * 0.65 + ripple2 * 0.35) * _RippleStrength * 0.04 * _Strength;
                float3 positionWS = TransformObjectToWorld(pos);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;
                output.positionWS = positionWS;
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(positionWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 centered = input.uv * 2.0 - 1.0;
                float2 stretch = max(_Stretch.xy, float2(0.05, 0.05));
                float2 flow = normalize(_FlowDirection.xz + 1e-5);
                float2 aligned = float2(dot(centered, float2(flow.y, -flow.x)), dot(centered, flow));
                aligned.x /= stretch.y;
                aligned.y /= stretch.x;

                float radial = length(aligned);
                float edgeNoise = Fbm(aligned * _EdgeNoiseScale + _Time.y * 0.2);
                float edge = radial + (edgeNoise - 0.5) * _EdgeNoiseStrength;
                float mask = 1.0 - smoothstep(1.0 - _EdgeSoftness, 1.0, edge);
                clip(mask - 0.001);

                float2 flowUv = aligned;
                flowUv += flow * (_Time.y * _FlowMotion * 0.15);
                float n1 = Fbm(flowUv * 3.5 + _Time.y * _RippleSpeed * 0.25);
                float n2 = Fbm(flowUv * 6.2 - float2(_Time.y * _RippleSpeed * 0.18, 0.0) + 8.3);
                float surface = lerp(n1, n2, 0.5);

                float foamBand = smoothstep(
                    1.0 - _FoamEdgeWidth - _EdgeSoftness,
                    1.0 - _FoamEdgeWidth * 0.2,
                    edge);
                float2 foamUv = aligned * _FoamNoiseScale;
                foamUv += float2(_Time.y * _FoamScrollSpeed, -_Time.y * _FoamScrollSpeed * 0.7);
                float foamNoise = Fbm(foamUv);
                float foamDetail = saturate((foamNoise - 0.35) * 2.2);
                float foam = foamBand * foamDetail * _FoamStrength * _Strength;

                float3 normalTS = normalize(float3(
                    (n1 - 0.5) * _NormalStrength,
                    1.0,
                    (n2 - 0.5) * _NormalStrength));
                float3 normalWS = normalize(float3(normalTS.x, normalTS.y, normalTS.z));

                float3 viewDir = normalize(input.viewDirWS);
                float ndotv = saturate(dot(normalWS, viewDir));
                float fresnel = pow(1.0 - ndotv, _FresnelPower) * _FresnelIntensity * mask;

                float3 lightDir = normalize(float3(0.25, 0.9, 0.2));
                float3 halfDir = normalize(lightDir + viewDir);
                float spec = pow(saturate(dot(normalWS, halfDir)), 56.0) * _SpecularIntensity * mask;

                float ripplePulse = 0.5 + 0.5 * sin((aligned.x * 10.0 + aligned.y * 8.0) + _Time.y * _RippleSpeed * 6.283185);
                float3 waterColor = lerp(_BaseColor.rgb * 0.78, _BaseColor.rgb * 1.15, surface);
                waterColor = lerp(waterColor, waterColor * 1.08, ripplePulse * _RippleStrength);
                waterColor = lerp(waterColor, _FoamColor.rgb, saturate(foam));
                waterColor += fresnel * float3(0.65, 0.82, 1.0);
                waterColor += spec * float3(0.95, 0.98, 1.0);

                float alpha = _BaseColor.a * _Opacity * _Strength * mask;
                alpha = saturate(alpha + foam * 0.55 + fresnel * 0.1);
                return half4(waterColor, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
