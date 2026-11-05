Shader "ForestPathPrototype/Shallow Caustics"
{
 Properties { _SurfaceHeight("Water Surface Height",Float)=1.2 _Color("Light color",Color)=(.24,.65,.55,1) _Strength("Strength",Range(0,1))=.23 _Scale("Pattern size",Float)=.32 _Speed("Flow speed",Float)=.16 }
 SubShader {
 Tags { "Queue"="Transparent+5" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
 Blend SrcAlpha One ZWrite Off Cull Off
 Pass {
 Tags {"LightMode"="UniversalForward"}
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fog
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 CBUFFER_START(UnityPerMaterial)
 half4 _Color; float _SurfaceHeight;float _Strength,_Scale,_Speed;
 CBUFFER_END
 struct A {float4 positionOS:POSITION;};
 struct V {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;half fog:TEXCOORD1;};
 V vert(A i){V o;float3 p=TransformObjectToWorld(i.positionOS.xyz);o.world=p;o.positionCS=TransformWorldToHClip(p);o.fog=ComputeFogFactor(o.positionCS.z);return o;}
 half4 frag(V i):SV_Target {
 float2 p=i.world.xz*_Scale;float t=_Time.y*_Speed;
 float a=sin(p.x*3.1+sin(p.y*2.0+t)*1.15+t);
 float b=sin(p.y*2.8+sin(p.x*2.1-t*.7)*1.0-t*.8);
 float patternValue=pow(saturate(1-abs(a+b)),5);
 half3 col=MixFogColor(_Color.rgb,half3(0,0,0),i.fog);
 float underwater=1-smoothstep(_SurfaceHeight-.6,_SurfaceHeight+.15,_WorldSpaceCameraPos.y);
 return half4(col,patternValue*_Strength*underwater);
 }
 ENDHLSL
 }
 }
}
