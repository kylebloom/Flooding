Shader "Kyle/Flooding/Ingress Jet"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.35, 0.7, 0.95, 0.75)
        _Opacity ("Opacity", Range(0, 1)) = 0.75
        _FlowSpeed ("Flow Speed", Float) = 2.5
        _Turbulence ("Turbulence", Range(0, 2)) = 0.35
        _Strength ("Strength", Range(0, 1)) = 1
        _EdgeFade ("Edge Fade", Range(0.01, 0.5)) = 0.2
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
            Name "IngressJet"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _Opacity;
                float _FlowSpeed;
                float _Turbulence;
                float _Strength;
                float _EdgeFade;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 pos = input.positionOS.xyz;
                float n = Hash21(input.uv + _Time.y * 0.15) * 2.0 - 1.0;
                pos += input.normalOS * (n * _Turbulence * 0.02 * _Strength);
                output.positionCS = TransformObjectToHClip(pos);
                output.uv = input.uv;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float scroll = frac(input.uv.y * 3.0 - _Time.y * _FlowSpeed);
                float layerA = Hash21(float2(input.uv.x * 8.0, scroll * 6.0));
                float layerB = Hash21(float2(input.uv.x * 11.0 + 2.3, scroll * 9.0 + 1.7));
                float noise = lerp(layerA, layerB, 0.5);
                float radial = abs(input.uv.x - 0.5) * 2.0;
                float edge = saturate(1.0 - smoothstep(1.0 - _EdgeFade, 1.0, radial));
                float alpha = _BaseColor.a * _Opacity * _Strength * edge;
                alpha *= lerp(0.65, 1.0, noise);
                float3 color = _BaseColor.rgb * lerp(0.75, 1.15, noise);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
