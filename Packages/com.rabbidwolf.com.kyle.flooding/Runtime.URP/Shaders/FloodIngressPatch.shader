Shader "Kyle/Flooding/Ingress Patch"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.18, 0.5, 0.82, 0.55)
        _Opacity ("Opacity", Range(0, 1)) = 0.55
        _Strength ("Strength", Range(0, 1)) = 1
        _EdgeNoiseScale ("Edge Noise Scale", Float) = 2.4
        _EdgeNoiseStrength ("Edge Noise Strength", Range(0, 1)) = 0.35
        _EdgeSoftness ("Edge Softness", Range(0.01, 0.5)) = 0.18
        _RippleStrength ("Ripple Strength", Range(0, 1)) = 0.12
        _RippleSpeed ("Ripple Speed", Float) = 1.4
        _FoamStrength ("Foam Strength", Range(0, 1)) = 0.45
        _FoamEdgeWidth ("Foam Edge Width", Range(0.01, 0.4)) = 0.12
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
                float _FoamStrength;
                float _FoamEdgeWidth;
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

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 pos = input.positionOS.xyz;
                float2 centered = input.uv * 2.0 - 1.0;
                float ripple = sin((centered.x + centered.y) * 8.0 + _Time.y * _RippleSpeed * 6.283185);
                pos.y += ripple * _RippleStrength * 0.03 * _Strength;
                output.positionCS = TransformObjectToHClip(pos);
                output.uv = input.uv;
                output.positionWS = TransformObjectToWorld(pos);
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
                float noise = ValueNoise(aligned * _EdgeNoiseScale + _Time.y * 0.15);
                float edge = radial + (noise - 0.5) * _EdgeNoiseStrength;
                float mask = 1.0 - smoothstep(1.0 - _EdgeSoftness, 1.0, edge);
                clip(mask - 0.001);

                float foamBand = smoothstep(1.0 - _FoamEdgeWidth - _EdgeSoftness, 1.0 - _FoamEdgeWidth * 0.25, edge);
                float foam = foamBand * _FoamStrength * _Strength;

                float ripple = 0.5 + 0.5 * sin((aligned.x * 9.0 + aligned.y * 7.0) + _Time.y * _RippleSpeed * 6.283185);
                float3 color = lerp(_BaseColor.rgb * 0.85, _BaseColor.rgb * 1.1, ripple);
                color = lerp(color, float3(0.85, 0.92, 0.98), foam);

                float alpha = _BaseColor.a * _Opacity * _Strength * mask;
                alpha = saturate(alpha + foam * 0.15);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
