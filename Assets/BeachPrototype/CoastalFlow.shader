Shader "ForestPathPrototype/Coastal Flow"
{
 Properties {
 _ShallowColor("Shallow Water",Color)=(.08,.43,.40,1)
 _DeepColor("Deep Water",Color)=(.025,.18,.25,1)
 _UnderColor("Underwater Surface",Color)=(.025,.26,.30,1)
 _WaveNormal("Ripple Normal",2D)="bump"{}
 _RippleScale("Ripple Tiling",Float)=.20
 _RippleStrength("Ripple Strength",Range(0,1))=.24
 _FlowSpeed("Flow Speed",Range(0,2))=1
 _WaveHeight("Small Wave Height",Range(0,.15))=.035
 _ShallowOpacity("Shallow Water Opacity",Range(0,1))=.68
 _ShoreFade("Shore Softness (metres)",Range(.1,3))=1.1
 _DepthRange("Colour Depth (metres)",Float)=4
 _FoamStrength("Shore Foam",Range(0,1))=.20
 _Glint("Sun Glint",Range(0,1))=.22
 _WaterLimits("Water Area X min/max Z min/max",Vector)=(80,230,-160,140)
 _BoundaryFade("Outer Boundary Fade",Float)=10
 }
 SubShader {
 Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
 Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass {
 Tags{"LightMode"="UniversalForward"}
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #pragma multi_compile_fog
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
 TEXTURE2D(_WaveNormal); SAMPLER(sampler_WaveNormal);
 CBUFFER_START(UnityPerMaterial)
 half4 _ShallowColor,_DeepColor,_UnderColor;
 float4 _WaterLimits; float _BoundaryFade;
 float4 _WaveNormal_ST;
 float _ShallowOpacity,_RippleScale,_RippleStrength,_FlowSpeed,_WaveHeight,_ShoreFade,_DepthRange,_FoamStrength,_Glint;
 CBUFFER_END
 struct A {float4 positionOS:POSITION;};
 struct V {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float eye:TEXCOORD1;half fog:TEXCOORD2;};
 V Vert(A v) {
 V o;float3 p=TransformObjectToWorld(v.positionOS.xyz);
 float t=_Time.y*_FlowSpeed;
 p.y+=_WaveHeight*(sin(p.x*.45+p.z*.25-t*1.15)+.45*sin(p.x*-.26+p.z*.62-t*1.6));
 o.world=p;o.positionCS=TransformWorldToHClip(p);o.eye=-TransformWorldToView(p).z;o.fog=ComputeFogFactor(o.positionCS.z);return o;
 }
 half4 Frag(V i, FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target {
 float t=_Time.y*_FlowSpeed;
 float2 uv=i.world.xz*_RippleScale;
 half3 n1=UnpackNormal(SAMPLE_TEXTURE2D(_WaveNormal,sampler_WaveNormal,uv+float2(.021,.009)*t));
 half3 n2=UnpackNormal(SAMPLE_TEXTURE2D(_WaveNormal,sampler_WaveNormal,float2(uv.y,-uv.x)*1.63+float2(-.015,.018)*t));
 half2 slope=(n1.xy+n2.xy*.6)*_RippleStrength;
 half3 n=normalize(half3(-slope.x,1,-slope.y));
 half3 view=SafeNormalize(GetWorldSpaceViewDir(i.world));
 bool top=IS_FRONT_VFACE(face,true,false);
 float depth=LinearEyeDepth(SampleSceneDepth(GetNormalizedScreenSpaceUV(i.positionCS)),_ZBufferParams);
 float gap=max(0,depth-i.eye);
 half fade=smoothstep(0,_ShoreFade,gap);
 half absorption=1-exp(-gap/max(.1,_DepthRange));
 half fresnel=pow(1-saturate(abs(dot(n,view))),5);
 half3 color=lerp(_ShallowColor.rgb,_DeepColor.rgb,absorption);
 Light sun=GetMainLight();
 half spec=pow(saturate(dot(n,SafeNormalize(sun.direction+view))),120)*_Glint;
 color=lerp(color,half3(.24,.42,.48),fresnel*.5);
 color+=sun.color*spec;
 float band=sin(gap*10-t*1.5+n1.x*3)*.5+.5;
 half foam=(1-smoothstep(.06,.55,gap))*smoothstep(.5,.85,band)*_FoamStrength;
 color=lerp(color,half3(.57,.75,.72),foam);
 half alpha=lerp(_ShallowOpacity,.94,absorption)*fade;
 alpha=max(alpha,fresnel*.72*fade);
 if(!top) {half window=pow(saturate(dot(-n,view)),4);color=lerp(_UnderColor.rgb*.65,half3(.13,.38,.42),window*.75);color+=half3(.025,.05,.055)*(n1.x+n2.y);alpha=.80; }
 float edge=min(min(i.world.x-_WaterLimits.x,_WaterLimits.y-i.world.x),min(i.world.z-_WaterLimits.z,_WaterLimits.w-i.world.z));
 alpha*=smoothstep(0,max(.01,_BoundaryFade),edge);
 return half4(MixFog(color,i.fog),alpha);
 }
 ENDHLSL
 }
 }
}
