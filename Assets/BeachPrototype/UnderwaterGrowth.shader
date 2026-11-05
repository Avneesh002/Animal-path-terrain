Shader "ForestPathPrototype/Underwater Growth"
{
 Properties { _BaseColor("Tint",Color)=(1,1,1,1) _Sway("Current Sway",Float)=0.2 }
 SubShader {
 Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
 Cull Off
 Pass {
 Tags { "LightMode"="UniversalForward" }
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #pragma multi_compile_fog
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 CBUFFER_START(UnityPerMaterial)
 half4 _BaseColor;
 float _Sway;
 CBUFFER_END
 struct A { float4 pos:POSITION; float3 normal:NORMAL; half4 color:COLOR; float2 uv:TEXCOORD0; };
 struct V { float4 pos:SV_POSITION; half4 color:COLOR; float3 normal:TEXCOORD0; half fog:TEXCOORD1; };
 V Vert(A i) {
 V o;
 float3 p=i.pos.xyz;
 p.x+=sin(_Time.y*.8+p.y*.75+p.z*.4)*_Sway*i.uv.y;
 p.z+=cos(_Time.y*.63+p.y*.6+p.x*.3)*_Sway*.45*i.uv.y;
 o.pos=TransformObjectToHClip(p);
 o.normal=TransformObjectToWorldNormal(i.normal);
 o.color=i.color*_BaseColor;
 o.fog=ComputeFogFactor(o.pos.z);
 return o;
 }
 half4 Frag(V i):SV_Target {
 Light l=GetMainLight();
 half ndl=abs(dot(normalize(i.normal),l.direction));
 half3 lit=i.color.rgb*(SampleSH(normalize(i.normal))*.6+l.color*(.25+ndl*.6));
 return half4(MixFog(lit,i.fog),1);
 }
 ENDHLSL
 }
 }
}
