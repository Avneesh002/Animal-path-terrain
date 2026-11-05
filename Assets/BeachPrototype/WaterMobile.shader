Shader "ForestPathPrototype/Water Mobile"
{
    Properties
    {
        _DeepColor ("Deep Water", Color) = (0.012, 0.12, 0.19, 1)
        _ShallowColor ("Shallow Water", Color) = (0.055, 0.31, 0.34, 1)
        _WaveNormal ("Wave Normal", 2D) = "bump" {}
        _WaveScale ("Wave Scale", Range(0.25, 2.0)) = 0.8
        _WaveSpeed ("Wave Speed", Range(0.0, 2.0)) = 0.85
        _NormalStrength ("Wave Relief", Range(0.0, 2.0)) = 0.8
        _Transparency ("Water Transparency", Range(0.35, 0.95)) = 0.78
        _EdgeFade ("Soft Shore Edge", Range(0.01, 0.12)) = 0.045
        _ColorVariation ("Water Color Variation", Range(0.0, 0.5)) = 0.18
        _SpecularStrength ("Sun Glint", Range(0.0, 2.0)) = 0.65
        _Smoothness ("Glint Sharpness", Range(8.0, 128.0)) = 40.0
        _FresnelStrength ("Horizon Sheen", Range(0.0, 0.5)) = 0.12
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Pass
        {
            Name "WaterForward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_WaveNormal);
            SAMPLER(sampler_WaveNormal);
            CBUFFER_START(UnityPerMaterial)
                half4 _DeepColor;
                half4 _ShallowColor;
                float4 _WaveNormal_ST;
                float _WaveScale;
                float _WaveSpeed;
                float _NormalStrength;
                float _Transparency;
                float _EdgeFade;
                float _ColorVariation;
                float _SpecularStrength;
                float _Smoothness;
                float _FresnelStrength;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float2 uv : TEXCOORD1; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = p.positionCS;
                output.positionWS = p.positionWS;
                output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                float t = _Time.y * _WaveSpeed;
                float2 uv = input.positionWS.xz * (0.055 * _WaveScale);
                float2 uv2 = float2(uv.x * 0.73 - uv.y * 0.41, uv.x * 0.41 + uv.y * 0.73);
                half3 n1 = UnpackNormal(SAMPLE_TEXTURE2D(_WaveNormal, sampler_WaveNormal, uv + t * float2(0.018, 0.011)));
                half3 n2 = UnpackNormal(SAMPLE_TEXTURE2D(_WaveNormal, sampler_WaveNormal, uv2 + t * float2(-0.012, 0.017)));
                half2 waveSlope = (n1.xy + n2.xy * 0.55h) * _NormalStrength;
                half3 normalWS = normalize(half3(-waveSlope.x, 1.0h, -waveSlope.y));
                if (!IS_FRONT_VFACE(face, true, false)) normalWS = -normalWS;
                half3 viewDir = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                Light mainLight = GetMainLight();
                half3 lightDir = normalize(mainLight.direction);
                half3 halfDir = SafeNormalize(lightDir + viewDir);
                float2 macroUV = input.positionWS.xz * 0.012;
                float mottle = 0.5 + 0.25 * (n1.x + n2.y) + 0.20 * sin(dot(macroUV, float2(1.0, 0.73)) - t * 0.08);
                half3 baseColor = lerp(_DeepColor.rgb, _ShallowColor.rgb, saturate(mottle) * _ColorVariation + 0.12h);
                half ndl = saturate(dot(normalWS, lightDir));
                half spec = pow(saturate(dot(normalWS, halfDir)), _Smoothness) * _SpecularStrength;
                half facing = saturate(dot(normalWS, viewDir));
                half fresnel = pow(1.0h - facing, 5.0h);
                half3 color = baseColor * (0.82h + 0.18h * ndl);
                color += mainLight.color * spec;
                color = lerp(color, _ShallowColor.rgb, fresnel * _FresnelStrength);

                // Fade the perimeter of this plane so the water eases into the beach instead of ending at a hard rectangle.
                float edgeDistance = min(min(input.uv.x, 1.0 - input.uv.x), min(input.uv.y, 1.0 - input.uv.y));
                half shoreFade = smoothstep(0.0, _EdgeFade, edgeDistance);
                half alpha = lerp(_Transparency, min(0.96, _Transparency + 0.16), fresnel) * shoreFade;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
