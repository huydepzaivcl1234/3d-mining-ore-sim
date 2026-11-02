Shader "Survivor/Experimental/CrowdVAT"
{
 Properties { _BaseMap("Texture",2D)="white"{} _PositionTex("Positions",2D)="black"{} _NormalTex("Normals",2D)="black"{} }
 SubShader {
 Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
 HLSLINCLUDE
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
 Texture2D<float4> _PositionTex; Texture2D<float4> _NormalTex;
 UNITY_INSTANCING_BUFFER_START(Props)
 UNITY_DEFINE_INSTANCED_PROP(float4,_AnimData)
 UNITY_DEFINE_INSTANCED_PROP(float4,_Tint)
 UNITY_INSTANCING_BUFFER_END(Props)
 struct A {float3 positionOS:POSITION; float2 uv:TEXCOORD0; float2 index:TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID};
 void Pose(A a,out float3 p,out float3 n) {
 float4 d=UNITY_ACCESS_INSTANCED_PROP(Props,_AnimData);int count=max(1,(int)abs(d.y));
 float time=_Time.y*d.w+d.z;float f=(d.y<0?clamp(time,0,.99999):frac(time))*count;
 int frame=(int)floor(f);int next=frame+1<count?frame+1:(d.y<0?count-1:0);int id=(int)a.index.x;
 p=lerp(_PositionTex.Load(int3(id,(int)d.x+frame,0)).xyz,_PositionTex.Load(int3(id,(int)d.x+next,0)).xyz,frac(f));
 n=lerp(_NormalTex.Load(int3(id,(int)d.x+frame,0)).xyz,_NormalTex.Load(int3(id,(int)d.x+next,0)).xyz,frac(f));
 }
 ENDHLSL
 Pass {
 Tags {"LightMode"="UniversalForward"}
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #pragma multi_compile_instancing
 #pragma multi_compile_fog
 #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
 #pragma multi_compile_fragment _ _SHADOWS_SOFT
 struct V {float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float3 normal:TEXCOORD1; float fog:TEXCOORD2; float3 world:TEXCOORD3;float4 tint:TEXCOORD4;};
 V Vert(A a) {
 UNITY_SETUP_INSTANCE_ID(a);float3 p,n;Pose(a,p,n);
 V o;o.world=TransformObjectToWorld(p);o.pos=TransformWorldToHClip(o.world);o.uv=a.uv;o.normal=TransformObjectToWorldNormal(n);o.fog=ComputeFogFactor(o.pos.z);o.tint=UNITY_ACCESS_INSTANCED_PROP(Props,_Tint);return o;
 }
 half4 Frag(V i):SV_Target {Light l=GetMainLight(TransformWorldToShadowCoord(i.world));half3 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb;c=lerp(c,c*i.tint.rgb,saturate(i.tint.a));half3 light=SampleSH(normalize(i.normal))+l.color*(.2+.8*saturate(dot(normalize(i.normal),l.direction))*l.shadowAttenuation);return half4(MixFog(c*max(light,.22),i.fog),1);}
 ENDHLSL
 }
 Pass {
 Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"}
 ZWrite On ZTest LEqual ColorMask 0
 HLSLPROGRAM
 #pragma vertex ShadowVert
 #pragma fragment ShadowFrag
 #pragma multi_compile_instancing
 #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
 float3 _LightDirection;float3 _LightPosition;
 struct S {float4 positionCS:SV_POSITION;};
 S ShadowVert(A a){UNITY_SETUP_INSTANCE_ID(a);float3 p,n;Pose(a,p,n);float3 world=TransformObjectToWorld(p);float3 normal=TransformObjectToWorldNormal(n);
 #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
 float3 direction=normalize(_LightPosition-world);
 #else
 float3 direction=_LightDirection;
 #endif
 S o;o.positionCS=ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(world,normal,direction)));return o;}
 half4 ShadowFrag(S i):SV_Target{return 0;}
 ENDHLSL
 }
 }
}
