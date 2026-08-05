Shader "Kyle/Flooding/Ingress Jet"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.35, 0.72, 0.98, 0.78)
        _Opacity ("Opacity", Range(0, 1)) = 0.78
        _FlowSpeed ("Flow Speed", Float) = 3.0
        _Turbulence ("Turbulence", Range(0, 2)) = 0.55
        _Strength ("Strength", Range(0, 1)) = 1
        _EdgeFade ("Edge Fade", Range(0.01, 0.6)) = 0.28
        _FresnelPower ("Fresnel Power", Range(0.5, 8)) = 3.5
        _FresnelIntensity ("Fresnel Intensity", Range(0, 2)) = 0.55
        _SpecularIntensity ("Specular Intensity", Range(0, 2)) = 0.45
        _FoamHighlight ("Foam Highlight", Range(0, 1)) = 0.35
        _AlphaBreakup ("Alpha Breakup", Range(0, 1)) = 0.45
        _Distortion ("Distortion", Range(0, 0.1)) = 0.02
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
                float _FresnelPower;
                float _FresnelIntensity;
                float _SpecularIntensity;
                float _FoamHighlight;
                float _AlphaBreakup;
                float _Distortion;
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
                float3 viewDirWS : TEXCOORD2;
                float3 positionWS : TEXCOORD3;
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
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
                p = p * 2.03 + 17.1;
                a *= 0.5;
                v += ValueNoise(p) * a;
                p = p * 2.07 + 9.3;
                a *= 0.5;
                v += ValueNoise(p) * a;
                return v;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 pos = input.positionOS.xyz;
                float2 flowUv = float2(input.uv.x * 4.0, input.uv.y * 3.0 - _Time.y * _FlowSpeed * 0.35);
                float n = Fbm(flowUv) * 2.0 - 1.0;
                float along = input.uv.y;
                float displace = n * _Turbulence * 0.045 * _Strength * lerp(0.55, 1.2, along);
                pos += input.normalOS * displace;

                float3 positionWS = TransformObjectToWorld(pos);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(positionWS);
                output.positionWS = positionWS;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float scrollA = uv.y * 4.5 - _Time.y * _FlowSpeed;
                float scrollB = uv.y * 7.0 - _Time.y * _FlowSpeed * 1.37;
                float layerA = Fbm(float2(uv.x * 6.0, scrollA));
                float layerB = Fbm(float2(uv.x * 9.5 + 3.1, scrollB + 1.7));
                float detail = lerp(layerA, layerB, 0.55);

                float radial = abs(uv.x - 0.5) * 2.0;
                float edgeNoise = (ValueNoise(float2(uv.x * 14.0, scrollA * 2.5)) - 0.5) * 0.22 * _Turbulence;
                float softEdge = saturate(1.0 - smoothstep(1.0 - _EdgeFade, 1.0, radial + edgeNoise));

                float breakup = lerp(1.0, detail, _AlphaBreakup);
                float tipFade = smoothstep(0.0, 0.12, uv.y) * smoothstep(1.0, 0.72, uv.y);
                float alpha = _BaseColor.a * _Opacity * _Strength * softEdge * breakup * tipFade;
                alpha *= lerp(0.55, 1.05, detail);

                float3 normalWS = normalize(input.normalWS);
                // Cheap screen-space wobble of the shading normal for turbulent look.
                float2 distortUv = uv * 8.0 + float2(_Time.y * _FlowSpeed * 0.2, -_Time.y * _FlowSpeed);
                float2 nOff = float2(
                    ValueNoise(distortUv) - 0.5,
                    ValueNoise(distortUv + 19.2) - 0.5) * _Distortion * 40.0 * _Strength;
                normalWS = normalize(normalWS + float3(nOff.x, 0.0, nOff.y));

                float3 viewDir = normalize(input.viewDirWS);
                float ndotv = saturate(dot(normalWS, viewDir));
                float fresnel = pow(1.0 - ndotv, _FresnelPower) * _FresnelIntensity;

                float3 lightDir = normalize(float3(0.35, 0.85, 0.25));
                float3 halfDir = normalize(lightDir + viewDir);
                float spec = pow(saturate(dot(normalWS, halfDir)), 48.0) * _SpecularIntensity;

                float foamNoise = Fbm(float2(uv.x * 12.0, scrollA * 1.8 + 4.0));
                float foam = saturate((foamNoise - 0.62) * 3.5) * _FoamHighlight * _Strength * softEdge;

                float3 deep = _BaseColor.rgb * 0.72;
                float3 bright = _BaseColor.rgb * 1.18;
                float3 color = lerp(deep, bright, detail);
                color = lerp(color, float3(0.9, 0.96, 1.0), foam);
                color += fresnel * float3(0.75, 0.88, 1.0);
                color += spec * float3(0.95, 0.98, 1.0);

                return half4(color, saturate(alpha + foam * 0.2 + fresnel * 0.08));
            }
            ENDHLSL
        }
    }

    FallBack Off
}
