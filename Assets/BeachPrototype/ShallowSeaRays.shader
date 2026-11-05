Shader "ForestPathPrototype/Shallow Sun Rays"
{
 Properties { _SurfaceHeight("Water Surface Height",Float)=1.2 _Color("Ray color",Color)=(.35,.75,.7,1) _Strength("Strength",Range(0,1))=.065 }
 SubShader {
 Tags { "Queue"="Transparent+10" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
 Blend SrcAlpha One ZWrite Off Cull Off
 Pass {
 Tags {"LightMode"="UniversalForward"}
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fog
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 CBUFFER_START(UnityPerMaterial)
 half4 _Color; float _SurfaceHeight; float _Strength;
 CBUFFER_END
 struct A {float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
 struct V {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half fog:TEXCOORD1;};
 V vert(A i){V o;float3 p=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(p);o.uv=i.uv;o.fog=ComputeFogFactor(o.positionCS.z);return o;}
 half4 frag(V i):SV_Target {
 float horizontal=pow(saturate(1-abs(i.uv.x*2-1)),1.5);
 float vertical=smoothstep(0,.3,i.uv.y)*smoothstep(1,.75,i.uv.y);
 float flicker=.8+.2*sin(_Time.y*.6+i.uv.y*3);
 return half4(MixFogColor(_Color.rgb,half3(0,0,0),i.fog),horizontal*vertical*flicker*_Strength*(1-smoothstep(_SurfaceHeight-.6,_SurfaceHeight+.15,_WorldSpaceCameraPos.y)));
 }
 ENDHLSL
 }
 }
}
